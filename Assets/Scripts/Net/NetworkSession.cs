using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;

/// <summary>
/// Owns the Netcode for GameObjects NetworkManager and the connect flow.
///
/// Hosting tries Unity Relay first (real 6-character join codes). If UGS isn't
/// configured yet -- or the relay call fails -- it silently falls back to a
/// direct LAN host, and the "code" shown is the host's ip:port. Joining accepts
/// either: a 6-char relay code, or an "ip:port" (or bare ip) target.
/// </summary>
public class NetworkSession : MonoBehaviour
{
    public static NetworkSession Instance { get; private set; }

    public enum SessionState { Idle, Hosting, Connecting, InLobby, Failed }

    public SessionState State { get; private set; } = SessionState.Idle;
    public string Address { get; private set; } = "";
    public bool AddressIsRelay { get; private set; }
    public string Error { get; private set; } = "";

    public LobbyState Lobby { get; private set; }
    public bool IsHost => Manager != null && Manager.IsListening && Manager.IsServer;
    public bool InLobby => State == SessionState.InLobby || State == SessionState.Hosting;
    public int PlayerCount => Lobby != null && Lobby.IsSpawned ? Lobby.Players.Count : (IsHost ? 1 : 0);

    /// <summary>Raised on every peer (host included) when the host starts the match.</summary>
    public event Action MatchStarted;

    private NetworkManager Manager;
    private UnityTransport Transport;
    private string localName = "Player";
    private bool servicesReady;
    private bool pendingLeave;
    private readonly Dictionary<ulong, string> namesByClient = new Dictionary<ulong, string>();

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
            Leave();
            Error = msg;
        }

        // client: once the lobby object has replicated we're officially in the lobby
        if (State == SessionState.Connecting && Lobby != null && Lobby.IsSpawned)
            State = SessionState.InLobby;
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
            PlayerPrefab = null              // no player prefab; see LobbyState
        };

        Manager.ConnectionApprovalCallback += OnConnectionApproval;
        Manager.OnClientConnectedCallback += OnClientConnected;
        Manager.OnClientDisconnectCallback += OnClientDisconnected;
        return Manager;
    }

    // ------------------------------------------------------------------ host
    public async void HostGame(string playerName)
    {
        localName = SanitizeName(playerName);
        Error = ""; Address = ""; AddressIsRelay = false;
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localName);

        if (await TryRelayHost())
        {
            AddressIsRelay = true;
        }
        else
        {
            // LAN / direct fallback
            Transport.SetConnectionData("0.0.0.0", NetConfig.DefaultPort);
            if (!Manager.StartHost())
            {
                Fail("Could not start host.");
                return;
            }
            AddressIsRelay = false;
        }

        State = SessionState.Hosting;
        SpawnLobby();
        AddOrUpdatePlayer(Manager.LocalClientId, localName);
        if (!AddressIsRelay) Address = LocalAddress() + ":" + NetConfig.DefaultPort;
    }

    async Task<bool> TryRelayHost()
    {
        try
        {
            if (!await EnsureServices()) return false;
            var allocation = await RelayService.Instance.CreateAllocationAsync(NetConfig.MaxPlayers - 1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Transport.SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, true);

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
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localName);

        target = (target ?? "").Trim();
        bool looksLikeCode = target.Length == NetConfig.RelayCodeLength && !target.Contains(":") && !target.Contains(".");

        if (looksLikeCode)
        {
            AddressIsRelay = true;
            if (!await JoinViaRelay(target))
            {
                if (string.IsNullOrEmpty(Error)) Fail("Could not join that code.");
                return;
            }
        }
        else
        {
            if (!TryParseAddress(target, out string ip, out ushort port))
            {
                Fail("Enter a 6-character join code, or an address like 192.168.1.20:7777");
                return;
            }
            Transport.SetConnectionData(ip, port);
            if (!Manager.StartClient())
            {
                Fail("Could not start client.");
                return;
            }
        }

        Address = target;
        State = SessionState.Connecting;
    }

    async Task<bool> JoinViaRelay(string code)
    {
        try
        {
            if (!await EnsureServices()) return false;
            var join = await RelayService.Instance.JoinAllocationAsync(code.ToUpperInvariant());
            Transport.SetClientRelayData(
                join.RelayServer.IpV4, (ushort)join.RelayServer.Port,
                join.AllocationIdBytes, join.Key, join.ConnectionData, join.HostConnectionData, true);
            return Manager.StartClient();
        }
        catch (Exception e)
        {
            Fail("Join failed: " + e.Message);
            return false;
        }
    }

    // ----------------------------------------------------------------- lobby
    void SpawnLobby()
    {
        if (Lobby != null) return;
        GameObject go = new GameObject("Lobby");
        DontDestroyOnLoad(go);
        go.AddComponent<NetworkObject>();
        Lobby = go.AddComponent<LobbyState>();
        go.GetComponent<NetworkObject>().Spawn();
    }

    public void StartMatch()
    {
        if (!IsHost || Lobby == null || !Lobby.IsSpawned) return;
        Lobby.MatchStarted.Value = true;
        Lobby.BeginMatchClientRpc();
        NotifyMatchStarted();
    }

    public static void NotifyMatchStarted()
    {
        if (Instance != null) Instance.MatchStarted?.Invoke();
    }

    public void SetLobby(LobbyState lobby) { Lobby = lobby; }
    public void ClearLobby(LobbyState lobby) { if (Lobby == lobby) Lobby = null; }

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
        if (!Manager.IsServer) return;
        string name;
        if (!namesByClient.TryGetValue(id, out name)) name = "Player " + id;
        AddOrUpdatePlayer(id, name);
    }

    void OnClientDisconnected(ulong id)
    {
        if (Manager == null) return;

        if (Manager.IsServer)
        {
            RemovePlayer(id);
            namesByClient.Remove(id);
        }

        // A client that loses the host returns to the menu.
        if (!Manager.IsServer && id == Manager.LocalClientId && State != SessionState.Idle)
        {
            Error = "Disconnected from host.";
            pendingLeave = true;
        }
    }

    void AddOrUpdatePlayer(ulong id, string name)
    {
        if (Lobby == null || !Lobby.IsServer) return;
        for (int i = 0; i < Lobby.Players.Count; i++)
        {
            if (Lobby.Players[i].ClientId == id)
            {
                var updated = Lobby.Players[i];
                updated.Name = name;
                Lobby.Players[i] = updated;
                return;
            }
        }
        Lobby.Players.Add(new LobbyPlayerInfo { ClientId = id, Name = name });
    }

    void RemovePlayer(ulong id)
    {
        if (Lobby == null || !Lobby.IsServer) return;
        for (int i = Lobby.Players.Count - 1; i >= 0; i--)
        {
            if (Lobby.Players[i].ClientId == id) Lobby.Players.RemoveAt(i);
        }
    }

    // ----------------------------------------------------------------- leave
    public void Leave()
    {
        if (Manager != null && Manager.IsListening) Manager.Shutdown();

        if (Lobby != null) { Destroy(Lobby.gameObject); Lobby = null; }

        if (Manager != null) Destroy(Manager.gameObject);
        Manager = null; Transport = null;

        namesByClient.Clear();
        State = SessionState.Idle;
        Address = ""; AddressIsRelay = false;
    }

    void Fail(string message)
    {
        Error = message;
        State = SessionState.Failed;
        Leave();
        Error = message;
        State = SessionState.Failed;
    }

    // ----------------------------------------------------------------- utils
    async Task<bool> EnsureServices()
    {
        if (servicesReady) return true;
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            servicesReady = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("[net] Unity Services unavailable (Relay disabled): " + e.Message);
            return false;
        }
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
