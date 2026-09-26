using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Owns the Netcode for GameObjects NetworkManager and the connect flow.
///
/// Hosting tries Unity Relay first (real 6-character join codes). If UGS isn't
/// configured -- or the relay call fails -- it falls back to a direct LAN host,
/// and the "code" shown is the host's ip:port. Joining accepts either.
///
/// Lobby state is exchanged with custom named messages rather than a spawned
/// NetworkObject: the world is built from code with no prefabs, so registering
/// and hashing a network prefab is both unnecessary and failure-prone.
/// </summary>
public class NetworkSession : MonoBehaviour
{
    public static NetworkSession Instance { get; private set; }

    static class Msg
    {
        public const string Hello = "td.hello";
        public const string Lobby = "td.lobby";
        public const string Start = "td.start";
    }

    public enum SessionState { Idle, Hosting, Connecting, InLobby, Failed }

    public SessionState State { get; private set; } = SessionState.Idle;
    public string Address { get; private set; } = "";
    public bool AddressIsRelay { get; private set; }
    public string Error { get; private set; } = "";

    /// <summary>Host-chosen difficulty, replicated with the lobby roster.</summary>
    public Difficulty MatchDifficulty { get; private set; } = Difficulty.Normal;

    public void SetDifficulty(Difficulty d)
    {
        if (!IsHost) return;
        MatchDifficulty = d;
        BroadcastLobby();
    }

    /// <summary>Authoritative on the host; populated from messages on clients.</summary>
    public readonly List<LobbyPlayerInfo> Players = new List<LobbyPlayerInfo>();

    public bool IsHost => Manager != null && Manager.IsListening && Manager.IsServer;
    public bool InLobby => State == SessionState.Hosting || State == SessionState.InLobby;
    public int PlayerCount => Players.Count;
    public float ConnectingSeconds => State == SessionState.Connecting ? Time.time - connectStart : 0f;

    public event Action MatchStarted;
    public event Action LobbyChanged;

    // Match messages are forwarded to whoever registers (MatchSync), looked up
    // at delivery time so registration order doesn't matter.
    static readonly string[] matchNames = { "td.state", "td.boards", "td.snap", "td.relay" };
    static readonly Dictionary<string, Action<ulong, FastBufferReader>> named =
        new Dictionary<string, Action<ulong, FastBufferReader>>();

    public static void RegisterNamed(string name, Action<ulong, FastBufferReader> callback)
    {
        named[name] = callback;
    }

    public static ulong LocalClientId
    {
        get { return Instance != null && Instance.Manager != null ? Instance.Manager.LocalClientId : ulong.MaxValue; }
    }

    public static bool IsConnected(ulong id)
    {
        var m = Instance != null ? Instance.Manager : null;
        if (m == null || !m.IsListening) return false;
        if (m.IsServer && id == m.LocalClientId) return true;
        foreach (var c in m.ConnectedClientsIds) if (c == id) return true;
        return false;
    }

    public static void SendNamedToAll(string name, FastBufferWriter writer)
    {
        var m = Instance != null ? Instance.Manager : null;
        if (m != null && m.CustomMessagingManager != null)
            m.CustomMessagingManager.SendNamedMessageToAll(name, writer);
    }

    public static void SendNamedToServer(string name, FastBufferWriter writer,
                                         NetworkDelivery delivery = NetworkDelivery.ReliableSequenced)
    {
        var m = Instance != null ? Instance.Manager : null;
        if (m != null && m.CustomMessagingManager != null)
            m.CustomMessagingManager.SendNamedMessage(name, NetworkManager.ServerClientId, writer, delivery);
    }

    public static void SendNamedToClients(string name, IReadOnlyList<ulong> clientIds, FastBufferWriter writer,
                                          NetworkDelivery delivery = NetworkDelivery.ReliableSequenced)
    {
        var m = Instance != null ? Instance.Manager : null;
        if (m != null && m.CustomMessagingManager != null && clientIds != null && clientIds.Count > 0)
            m.CustomMessagingManager.SendNamedMessage(name, clientIds, writer, delivery);
    }

    /// <summary>Client ids to fan a snapshot out to: everyone but its owner and the host itself.</summary>
    public static void FillRelayTargets(List<ulong> into, ulong excludeOwner)
    {
        into.Clear();
        var m = Instance != null ? Instance.Manager : null;
        if (m == null || !m.IsListening || !m.IsServer) return;
        foreach (ulong id in m.ConnectedClientsIds)
        {
            if (id == excludeOwner) continue;
            if (id == m.LocalClientId) continue;   // the host applies snapshots directly
            into.Add(id);
        }
    }

    private NetworkManager Manager;
    private UnityTransport Transport;
    private string localName = "Player";
    private bool servicesReady;
    private bool servicesFailed;
    private bool handlersReady;
    private bool leaving;
    private bool pendingLeave;
    private float connectStart;
    private readonly Dictionary<ulong, string> namesByClient = new Dictionary<ulong, string>();

    const float ConnectTimeoutSeconds = 12f;

    public static NetworkSession Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("NetworkSession");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<NetworkSession>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (pendingLeave)
        {
            pendingLeave = false;
            string msg = Error;
            LeaveInternal();
            Error = msg;
            State = SessionState.Failed;
        }

        if (State == SessionState.Connecting && Time.time - connectStart > ConnectTimeoutSeconds)
            Fail("Timed out connecting to " + Address);
    }

    // ---------------------------------------------------------------- manager
    NetworkManager EnsureManager()
    {
        if (Manager != null) return Manager;

        GameObject go = new GameObject("NetworkManager");
        DontDestroyOnLoad(go);
        Manager = go.AddComponent<NetworkManager>();
        Transport = go.AddComponent<UnityTransport>();

        Manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = Transport,
            EnableSceneManagement = false,   // the world is built from code
            ConnectionApproval = true,       // lets us read the player's name
            PlayerPrefab = null              // no player prefab; lobby uses messages
        };

        Manager.ConnectionApprovalCallback += OnConnectionApproval;
        Manager.OnClientConnectedCallback += OnClientConnected;
        Manager.OnClientDisconnectCallback += OnClientDisconnected;
        EnsureHandlers();
        return Manager;
    }

    void EnsureHandlers()
    {
        if (handlersReady || Manager == null) return;
        var cm = Manager.CustomMessagingManager;
        if (cm == null) return;

        cm.RegisterNamedMessageHandler(Msg.Hello, OnHelloMessage);
        cm.RegisterNamedMessageHandler(Msg.Lobby, OnLobbyMessage);
        cm.RegisterNamedMessageHandler(Msg.Start, OnStartMessage);

        for (int i = 0; i < matchNames.Length; i++)
        {
            string n = matchNames[i];
            cm.RegisterNamedMessageHandler(n, (sender, reader) =>
            {
                Action<ulong, FastBufferReader> cb;
                if (named.TryGetValue(n, out cb) && cb != null) cb(sender, reader);
            });
        }

        handlersReady = true;
    }

    // ------------------------------------------------------------------ host
    public async void HostGame(string playerName)
    {
        localName = SanitizeName(playerName);
        Error = ""; Address = ""; AddressIsRelay = false;
        leaving = false;
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localName);

        bool started;
        if (await TryRelayHost())
        {
            AddressIsRelay = true;
            started = true;
        }
        else
        {
            Transport.UseWebSockets = false;
            Transport.SetConnectionData("0.0.0.0", NetConfig.DefaultPort);
            started = Manager.StartHost();
            if (started)
            {
                AddressIsRelay = false;
                Address = LocalAddress() + ":" + NetConfig.DefaultPort;
            }
        }

        if (!started)
        {
            Fail("Could not start hosting.");
            return;
        }

        State = SessionState.Hosting;
        EnsureHandlers();

        Players.Clear();
        namesByClient.Clear();
        namesByClient[Manager.LocalClientId] = localName;
        Players.Add(new LobbyPlayerInfo { ClientId = Manager.LocalClientId, Name = localName, IsHost = true });
        BroadcastLobby();
    }

    async Task<bool> TryRelayHost()
    {
        try
        {
            if (!await EnsureServices()) return false;
            var allocation = await RelayService.Instance.CreateAllocationAsync(NetConfig.MaxPlayers - 1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerEndpoint ep = PickEndpoint(allocation.ServerEndpoints, RelayConnectionType());
            if (ep == null) { Debug.Log("[net] Relay returned no usable endpoint."); return false; }
            // Hosts pass their own connection data as the host connection data
            // (passing null crashes RelayConnectionData.FromByteArray).
            ApplyRelayData(ep, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.ConnectionData, allocation.Key);

            if (!Manager.StartHost()) return false;
            Address = code;
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("[net] Relay host unavailable, falling back to LAN: " + e.Message);
            return false;
        }
    }

    // ------------------------------------------------------------------ join
    public async void JoinGame(string target, string playerName)
    {
        localName = SanitizeName(playerName);
        Error = ""; Address = ""; AddressIsRelay = false;
        leaving = false;
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localName);

        target = (target ?? "").Trim();
        bool looksLikeCode = target.Length == NetConfig.RelayCodeLength
                             && !target.Contains(":") && !target.Contains(".");

        bool started;
        if (looksLikeCode)
        {
            AddressIsRelay = true;
            started = await JoinViaRelay(target);
            if (!started && string.IsNullOrEmpty(Error)) Error = "Could not join that code.";
        }
        else
        {
            if (!TryParseAddress(target, out string ip, out ushort port))
            {
                Fail("Enter a 6-character join code, or an address like 192.168.1.20:7777");
                return;
            }
            Transport.UseWebSockets = false;
            Transport.SetConnectionData(ip, port);
            started = Manager.StartClient();
            if (!started) Error = "Could not start the client.";
        }

        if (!started)
        {
            Fail(string.IsNullOrEmpty(Error) ? "Could not connect." : Error);
            return;
        }

        EnsureHandlers();
        Address = target;
        connectStart = Time.time;
        State = SessionState.Connecting;
    }

    async Task<bool> JoinViaRelay(string code)
    {
        try
        {
            if (!await EnsureServices()) { Error = "Online play needs Unity Gaming Services (not set up yet)."; return false; }
            var join = await RelayService.Instance.JoinAllocationAsync(code.ToUpperInvariant());
            RelayServerEndpoint ep = PickEndpoint(join.ServerEndpoints, RelayConnectionType());
            if (ep == null) { Error = "Relay returned no usable endpoint."; return false; }
            ApplyRelayData(ep, join.AllocationIdBytes, join.ConnectionData, join.HostConnectionData, join.Key);
            return Manager.StartClient();
        }
        catch (Exception e)
        {
            Error = "Join failed: " + e.Message;
            return false;
        }
    }

    /// <summary>Relay connection type: WebSockets in the browser (no UDP), DTLS
    /// on desktop.</summary>
    static string RelayConnectionType()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return RelayServerEndpoint.ConnectionTypeWss;
#else
        return RelayServerEndpoint.ConnectionTypeDtls;
#endif
    }

    static RelayServerEndpoint PickEndpoint(List<RelayServerEndpoint> eps, string type)
    {
        if (eps == null) return null;
        for (int i = 0; i < eps.Count; i++)
            if (eps[i] != null && eps[i].ConnectionType == type) return eps[i];
        return null;
    }

    /// <summary>Builds Relay transport data from the endpoint matching the chosen
    /// connection type. The allocation's RelayServer.Port is the plain UDP port,
    /// so using it for a secure/WebSocket connection binds to the wrong port.</summary>
    void ApplyRelayData(RelayServerEndpoint ep, byte[] allocationId, byte[] connectionData, byte[] hostConnectionData, byte[] key)
    {
        bool ws = ep.ConnectionType == RelayServerEndpoint.ConnectionTypeWss;
        Transport.UseWebSockets = ws;
        Transport.SetRelayServerData(new RelayServerData(
            ep.Host, (ushort)ep.Port, allocationId, connectionData, hostConnectionData, key, ep.Secure, ws));
    }

    // ---------------------------------------------------------------- lobby
    public void StartMatch()
    {
        if (!IsHost) return;
        if (Manager.CustomMessagingManager != null)
        {
            using var writer = new FastBufferWriter(1, Allocator.Temp);
            Manager.CustomMessagingManager.SendNamedMessageToAll(Msg.Start, writer);
        }
        MatchStarted?.Invoke();
    }

    void BroadcastLobby()
    {
        LobbyChanged?.Invoke();
        if (!IsHost || Manager.CustomMessagingManager == null) return;

        using var writer = new FastBufferWriter(4 + NetConfig.MaxPlayers * 48, Allocator.Temp);
        writer.WriteValueSafe(Players.Count);
        writer.WriteValueSafe((byte)MatchDifficulty);
        for (int i = 0; i < Players.Count; i++)
        {
            writer.WriteValueSafe(Players[i].ClientId);
            writer.WriteValueSafe(Players[i].Name ?? "");
            writer.WriteValueSafe(Players[i].IsHost);
        }
        Manager.CustomMessagingManager.SendNamedMessageToAll(Msg.Lobby, writer);
    }

    void OnLobbyMessage(ulong sender, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int count);
        reader.ReadValueSafe(out byte diff);
        MatchDifficulty = (Difficulty)Mathf.Clamp(diff, 0, 3);
        Players.Clear();
        for (int i = 0; i < count && i < NetConfig.MaxPlayers; i++)
        {
            reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out string name);
            reader.ReadValueSafe(out bool isHost);
            Players.Add(new LobbyPlayerInfo { ClientId = id, Name = name, IsHost = isHost });
        }
        LobbyChanged?.Invoke();
        Debug.Log("[net] roster: " + Players.Count + " player(s)");
    }

    void OnStartMessage(ulong sender, FastBufferReader reader)
    {
        MatchStarted?.Invoke();
    }

    void OnHelloMessage(ulong sender, FastBufferReader reader)
    {
        if (IsHost) BroadcastLobby();
    }

    // ---------------------------------------------------------- connections
    void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
                              NetworkManager.ConnectionApprovalResponse response)
    {
        if (Manager.ConnectedClientsIds.Count >= NetConfig.MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "Lobby is full (" + NetConfig.MaxPlayers + " max)";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        string name = "Player";
        if (request.Payload != null && request.Payload.Length > 0)
            name = SanitizeName(Encoding.UTF8.GetString(request.Payload));
        namesByClient[request.ClientNetworkId] = name;
    }

    void OnClientConnected(ulong id)
    {
        if (leaving) return;

        if (Manager.IsServer)
        {
            string name;
            if (!namesByClient.TryGetValue(id, out name)) name = "Player " + id;
            bool exists = false;
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].ClientId == id) { exists = true; break; }
            if (!exists)
                Players.Add(new LobbyPlayerInfo { ClientId = id, Name = name, IsHost = id == Manager.LocalClientId });
            BroadcastLobby();
            Debug.Log("[net] client " + id + " connected (" + Players.Count + " in lobby)");
        }
        else if (id == Manager.LocalClientId)
        {
            // We're through to the host: announce ourselves so it sends the roster.
            State = SessionState.InLobby;
            Debug.Log("[net] connected to host as client " + id);
            if (Manager.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(1, Allocator.Temp);
                Manager.CustomMessagingManager.SendNamedMessage(Msg.Hello, NetworkManager.ServerClientId, writer);
            }
        }
    }

    void OnClientDisconnected(ulong id)
    {
        if (leaving || Manager == null) return;

        if (Manager.IsServer)
        {
            for (int i = Players.Count - 1; i >= 0; i--)
                if (Players[i].ClientId == id) Players.RemoveAt(i);
            namesByClient.Remove(id);
            BroadcastLobby();
            Debug.Log("[net] client " + id + " left");
        }
        else if (id == Manager.LocalClientId)
        {
            Error = "Disconnected from host.";
            pendingLeave = true;
        }
    }

    // ----------------------------------------------------------------- leave
    public void Leave()
    {
        LeaveInternal();
        Error = ""; Address = ""; AddressIsRelay = false;
        State = SessionState.Idle;
    }

    void LeaveInternal()
    {
        leaving = true;
        handlersReady = false;

        if (Manager != null && Manager.IsListening) Manager.Shutdown();
        if (Manager != null) Destroy(Manager.gameObject);
        Manager = null; Transport = null;

        Players.Clear();
        namesByClient.Clear();
    }

    void Fail(string message)
    {
        LeaveInternal();
        Error = message;
        State = SessionState.Failed;
    }

    // ----------------------------------------------------------------- utils
    async Task<bool> EnsureServices()
    {
        if (servicesReady) return true;
        if (servicesFailed) return false;

        try
        {
            Task work = InitServicesAsync();
            Task done = await Task.WhenAny(work, Task.Delay(6000));
            if (done != work)
            {
                servicesFailed = true;
                Debug.Log("[net] Unity Services init timed out; using LAN.");
                return false;
            }
            await work;
            servicesReady = true;
            return true;
        }
        catch (Exception e)
        {
            servicesFailed = true;
            Debug.Log("[net] Unity Services unavailable (Relay disabled): " + e.Message);
            return false;
        }
    }

    static async Task InitServicesAsync()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    static string SanitizeName(string n)
    {
        n = (n ?? "").Trim();
        if (string.IsNullOrEmpty(n)) n = "Player";
        if (n.Length > 24) n = n.Substring(0, 24);
        return n;
    }

    static bool TryParseAddress(string target, out string ip, out ushort port)
    {
        ip = ""; port = NetConfig.DefaultPort;
        if (string.IsNullOrEmpty(target)) return false;

        string[] parts = target.Split(':');
        string host = parts[0].Trim();
        if (string.IsNullOrEmpty(host)) return false;
        if (parts.Length > 1 && !ushort.TryParse(parts[1].Trim(), out port)) return false;
        ip = host;
        return true;
    }

    static string LocalAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var addr in host.AddressList)
                if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                    return addr.ToString();
        }
        catch { /* fall through */ }
        return "127.0.0.1";
    }
}
