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
        public const string Theme = "td.theme";   // client -> host: chosen board
    }

    public enum SessionState { Idle, Hosting, Connecting, InLobby, Reconnecting, Failed }

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

    /// <summary>This peer's chosen board theme (index into
    /// <see cref="TDBoardBuilder.PickerThemes"/>). Kept in step with
    /// <see cref="TDGameManager.ActiveTheme"/> and replicated via the roster so
    /// every player can see it and remote boards render the right theme.</summary>
    public int LocalTheme { get; private set; }

    /// <summary>Publishes this peer's board choice. On the host it updates the
    /// local roster row directly; on a client it messages the host (optimistically
    /// updating the local copy first so the lobby reacts instantly).</summary>
    public void SetTheme(int theme)
    {
        LocalTheme = theme;
        ApplyLocalTheme(theme);

        if (IsHost) { BroadcastLobby(); return; }

        if (Manager == null || Manager.CustomMessagingManager == null || !LinkUp) return;
        using var w = new FastBufferWriter(4, Allocator.Temp);
        w.WriteValueSafe((byte)theme);
        Manager.CustomMessagingManager.SendNamedMessage(Msg.Theme, NetworkManager.ServerClientId, w);
    }

    void ApplyLocalTheme(int theme)
    {
        ulong me = LocalClientId;
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId != me) continue;
            var p = Players[i];
            p.Theme = theme;
            Players[i] = p;
            break;
        }
        LobbyChanged?.Invoke();
    }

    /// <summary>Authoritative on the host; populated from messages on clients.</summary>
    public readonly List<LobbyPlayerInfo> Players = new List<LobbyPlayerInfo>();

    public bool IsHost => Manager != null && Manager.IsListening && Manager.IsServer;
    public bool InLobby => State == SessionState.Hosting || State == SessionState.InLobby;
    /// <summary>Any live session, including a mid-match link outage that is
    /// being retried. Sync loops (MatchSync/SpectateSync/FxSync/ChatSync) run
    /// under this so the local board keeps simulating while disconnected.</summary>
    public bool InSession => State == SessionState.Hosting || State == SessionState.InLobby
                             || State == SessionState.Reconnecting;
    public bool Reconnecting => State == SessionState.Reconnecting;
    /// <summary>True when named messages can actually go out: listening, and
    /// (as a client) still connected. All SendNamed* entry points check this,
    /// so gameplay code can publish blindly during an outage.</summary>
    public bool LinkUp
    {
        get
        {
            if (Manager == null || !Manager.IsListening) return false;
            if (Manager.IsServer) return true;
            return Manager.IsConnectedClient;
        }
    }
    public int PlayerCount => Players.Count;
    public float ConnectingSeconds => State == SessionState.Connecting ? Time.time - connectStart : 0f;
    /// <summary>Seconds spent in Reconnecting (unscaled: the sim clock may be paused).</summary>
    public float ReconnectingSeconds => State == SessionState.Reconnecting ? Time.unscaledTime - reconnectStart : 0f;

    public event Action MatchStarted;
    public event Action LobbyChanged;
    /// <summary>Fired on a client that just re-established its transport
    /// mid-match (new ClientId, same seat). The game resyncs without resetting.</summary>
    public event Action Reconnected;

    // Match/chat messages are forwarded to whoever registers (MatchSync,
    // SpectateSync, ChatSync), looked up at delivery time so registration order
    // doesn't matter.
    static readonly string[] matchNames = { "td.state", "td.boards", "td.snap", "td.relay", "td.chat", "td.chatall" };
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
        var ns = Instance;
        if (ns == null || !ns.LinkUp) return;
        var m = ns.Manager;
        if (m != null && m.CustomMessagingManager != null)
            m.CustomMessagingManager.SendNamedMessageToAll(name, writer);
    }

    public static void SendNamedToServer(string name, FastBufferWriter writer,
                                         NetworkDelivery delivery = NetworkDelivery.ReliableSequenced)
    {
        var ns = Instance;
        if (ns == null || !ns.LinkUp) return;
        var m = ns.Manager;
        if (m != null && m.CustomMessagingManager != null)
            m.CustomMessagingManager.SendNamedMessage(name, NetworkManager.ServerClientId, writer, delivery);
    }

    public static void SendNamedToClients(string name, IReadOnlyList<ulong> clientIds, FastBufferWriter writer,
                                          NetworkDelivery delivery = NetworkDelivery.ReliableSequenced)
    {
        var ns = Instance;
        if (ns == null || !ns.LinkUp) return;
        var m = ns.Manager;
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
    /// <summary>Stable per-install identity, so a rejoin maps back to its seat
    /// even though NGO assigns a fresh ClientId every connect.</summary>
    private readonly Dictionary<ulong, string> guidByClient = new Dictionary<ulong, string>();
    /// <summary>True once Start (lobby -> match) has fired. While set, a local
    /// transport drop parks in Reconnecting (sim keeps running) instead of
    /// tearing the session down, and the host tombstones rather than prunes.</summary>
    private bool matchLive;

    // Reconnect loop (mid-match client only): retry the last address every few
    // seconds until ReconnectGraceSeconds elapses, then give up to Failed.
    private bool reconnectBusy;
    private float reconnectTimer;
    private float reconnectStart;
    private float reconnectAttemptStart;
    private int reconnectAttemptId;

    public const float ReconnectGraceSeconds = 300f;
    const float ReconnectRetrySeconds = 4f;

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

        // Mid-match link outage: keep retrying the last address until the
        // grace period elapses (unscaled time: the render loop may be paused).
        if (State == SessionState.Reconnecting)
        {
            if (Time.unscaledTime - reconnectStart >= ReconnectGraceSeconds)
            {
                Fail("Could not reconnect to the match.");
                return;
            }
            if (reconnectBusy)
            {
                if (Time.unscaledTime - reconnectAttemptStart > ConnectTimeoutSeconds)
                {
                    Debug.LogWarning("[net] reconnect attempt " + reconnectAttemptId + " timed out; resetting transport");
                    reconnectAttemptId++; // invalidate any Relay join still awaiting a response
                    reconnectBusy = false;
                    reconnectTimer = ReconnectRetrySeconds;
                    StopReconnectTransport();
                }
            }
            else
            {
                reconnectTimer -= Time.unscaledDeltaTime;
                // NGO's Shutdown is deferred to a network update. StartClient
                // while it is still listening would just fail on every retry.
                if (reconnectTimer <= 0f && Manager != null &&
                    !Manager.IsListening && !Manager.ShutdownInProgress)
                    TryReconnect();
            }
        }
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
        Manager.OnTransportFailure += OnTransportFailure;
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
        cm.RegisterNamedMessageHandler(Msg.Theme, OnThemeMessage);

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

    void OnTransportFailure()
    {
        Debug.LogWarning("[net] transport failure; host=" + IsHost + " state=" + State +
                         " t=" + Time.unscaledTime.ToString("0.0"));
    }

    // ------------------------------------------------------------------ host
    public async void HostGame(string playerName)
    {
        localName = SanitizeName(playerName);
        Error = ""; Address = ""; AddressIsRelay = false;
        leaving = false;
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ConnectionPayload(localName, LocalGuid));

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
        LocalTheme = (int)TDGameManager.ActiveTheme;
        Players.Add(new LobbyPlayerInfo { ClientId = Manager.LocalClientId, Name = localName, IsHost = true, Theme = LocalTheme });
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
        LocalTheme = (int)TDGameManager.ActiveTheme;
        Error = ""; Address = ""; AddressIsRelay = false;
        leaving = false;
        EnsureManager();
        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ConnectionPayload(localName, LocalGuid));

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
        matchLive = true;
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

        using var writer = new FastBufferWriter(8 + NetConfig.MaxPlayers * 56, Allocator.Temp);
        writer.WriteValueSafe(Players.Count);
        writer.WriteValueSafe((byte)MatchDifficulty);
        for (int i = 0; i < Players.Count; i++)
        {
            writer.WriteValueSafe(Players[i].ClientId);
            writer.WriteValueSafe(Players[i].Name ?? "");
            writer.WriteValueSafe(Players[i].IsHost);
            writer.WriteValueSafe((byte)Players[i].Theme);
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
            byte theme = 0;
            if (reader.Length - reader.Position >= 1) reader.ReadValueSafe(out theme);
            Players.Add(new LobbyPlayerInfo { ClientId = id, Name = name, IsHost = isHost, Theme = theme });
        }
        LobbyChanged?.Invoke();
        Debug.Log("[net] roster: " + Players.Count + " player(s)");
    }

    void OnStartMessage(ulong sender, FastBufferReader reader)
    {
        matchLive = true;   // clients learn the match began here (drives reconnect, not lobby)
        MatchStarted?.Invoke();
    }

    void OnHelloMessage(ulong sender, FastBufferReader reader)
    {
        if (!IsHost) return;
        // The hello carries the joiner's board choice; adopt it before the first
        // roster broadcast so nobody sees a default and then a flip.
        if (reader.Length - reader.Position >= 1)
        {
            reader.ReadValueSafe(out byte theme);
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].ClientId != sender) continue;
                var p = Players[i];
                p.Theme = theme;
                Players[i] = p;
                break;
            }
        }
        BroadcastLobby();
    }

    /// <summary>Host side of a lobby board change.</summary>
    void OnThemeMessage(ulong sender, FastBufferReader reader)
    {
        if (!IsHost) return;
        if (reader.Length - reader.Position < 1) return;
        reader.ReadValueSafe(out byte theme);
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId != sender) continue;
            var p = Players[i];
            p.Theme = theme;
            Players[i] = p;
            break;
        }
        BroadcastLobby();
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

        // Payload is "<version>\n<guid>\n<name>". Refuse a protocol mismatch here, in
        // the connection handshake, so a stale client never reaches the lobby.
        // The guid is a stable per-install identity: a mid-match rejoin maps
        // back to its seat even though NGO assigns a fresh ClientId.
        string payload = "";
        if (request.Payload != null && request.Payload.Length > 0)
            payload = Encoding.UTF8.GetString(request.Payload);
        int first = payload.IndexOf('\n');
        string version = first >= 0 ? payload.Substring(0, first).Trim() : "";
        string rest = first >= 0 ? payload.Substring(first + 1) : payload;
        int second = rest.IndexOf('\n');
        string guid = second >= 0 ? rest.Substring(0, second).Trim() : "";
        string name = second >= 0 ? rest.Substring(second + 1) : rest;

        if (version != NetConfig.FullVersion)
        {
            response.Approved = false;
            response.Reason = "Version mismatch: host " + NetConfig.FullVersion +
                              ", you " + (string.IsNullOrEmpty(version) ? "unknown" : version) +
                              " - both players must update";
            Debug.Log("[net] refused connection " + request.ClientNetworkId + ": " + response.Reason);
            return;
        }

        namesByClient[request.ClientNetworkId] = SanitizeName(name);
        guidByClient[request.ClientNetworkId] = guid ?? "";
    }

    /// <summary>Stable identity the host uses to recognise a rejoining peer.</summary>
    public string GuidForClient(ulong id)
    {
        string g;
        return guidByClient.TryGetValue(id, out g) ? g : "";
    }

    void OnClientConnected(ulong id)
    {
        if (leaving) return;

        if (Manager.IsServer)
        {
            string name;
            if (!namesByClient.TryGetValue(id, out name)) name = "Player " + id;
            string guid = GuidForClient(id);
            bool exists = false;
            for (int i = 0; i < Players.Count; i++)
            {
                // A mid-match rejoin carries a fresh ClientId but the same
                // install guid: adopt it into the kept seat so nobody's slot
                // (board position) shifts.
                if (Players[i].ClientId == id ||
                    (!string.IsNullOrEmpty(guid) && Players[i].Guid == guid))
                {
                    var p = Players[i];
                    p.ClientId = id;
                    p.Name = name;
                    p.Guid = guid;
                    Players[i] = p;
                    exists = true;
                    break;
                }
            }
            if (!exists)
                Players.Add(new LobbyPlayerInfo { ClientId = id, Name = name, IsHost = id == Manager.LocalClientId, Guid = guid });
            if (!matchLive) BroadcastLobby();
            Debug.Log("[net] client " + id + " connected (" + Players.Count +
                      " in lobby) t=" + Time.unscaledTime.ToString("0.0"));
        }
        else if (id == Manager.LocalClientId)
        {
            // We're through to the host: announce ourselves so it sends the roster.
            bool wasRejoin = State == SessionState.Reconnecting;
            State = SessionState.InLobby;
            reconnectBusy = false;
            reconnectAttemptId++;
            Debug.Log("[net] connected to host as client " + id + (wasRejoin ? " (rejoin)" : ""));
            if (Manager.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(4, Allocator.Temp);
                writer.WriteValueSafe((byte)LocalTheme);
                Manager.CustomMessagingManager.SendNamedMessage(Msg.Hello, NetworkManager.ServerClientId, writer);
            }
            // The game resyncs from live state (no wave reset, no board reset).
            if (wasRejoin) Reconnected?.Invoke();
        }
    }

    void OnClientDisconnected(ulong id)
    {
        if (leaving || Manager == null) return;

        if (Manager.IsServer)
        {
            // Mid-match the roster seat is kept (the board row is tombstoned by
            // MatchSync instead), so survivors' slots never shift. Pre-match a
            // departure still removes the seat as before.
            if (matchLive) { Debug.Log("[net] client " + id + " dropped mid-match (seat held) t=" +
                                       Time.unscaledTime.ToString("0.0")); return; }
            for (int i = Players.Count - 1; i >= 0; i--)
                if (Players[i].ClientId == id) Players.RemoveAt(i);
            namesByClient.Remove(id);
            guidByClient.Remove(id);
            BroadcastLobby();
            Debug.Log("[net] client " + id + " left");
        }
        else if (id == Manager.LocalClientId)
        {
            if (matchLive)
            {
                string rejoinReason = Manager.DisconnectReason;
                if (!string.IsNullOrEmpty(rejoinReason) && rejoinReason.StartsWith("Version mismatch:", StringComparison.Ordinal))
                {
                    Error = rejoinReason;
                    pendingLeave = true; // leave outside NGO's disconnect callback
                    return;
                }
                // Our board keeps simulating locally (per-peer sim needs no
                // link); park in Reconnecting and retry the last address.
                Debug.Log("[net] link lost mid-match - holding board, retrying " + Address +
                          " reason=" + (string.IsNullOrEmpty(rejoinReason) ? "transport" : rejoinReason));
                EnterReconnecting();
                return;
            }
            // A refused join (e.g. version mismatch) supplies a reason; surface
            // it instead of the generic disconnect text.
            string reason = Manager != null ? Manager.DisconnectReason : null;
            Error = string.IsNullOrEmpty(reason) ? "Disconnected from host." : reason;
            pendingLeave = true;
        }
    }

    /// <summary>Parks the transport in Reconnecting without destroying the
    /// manager, handlers, roster or world: the local sim keeps running and
    /// sends drop silently (LinkUp) until the link is back.</summary>
    void EnterReconnecting()
    {
        bool firstDrop = State != SessionState.Reconnecting;
        reconnectAttemptId++;
        StopReconnectTransport();
        State = SessionState.Reconnecting;
        reconnectBusy = false;
        reconnectTimer = firstDrop ? 0f : ReconnectRetrySeconds;
        if (firstDrop) reconnectStart = Time.unscaledTime;
    }

    void StopReconnectTransport()
    {
        // NGO discards CustomMessagingManager during shutdown. The next client
        // start must register all named handlers against its replacement.
        handlersReady = false;
        if (Manager != null && Manager.IsListening && !Manager.ShutdownInProgress)
            Manager.Shutdown(true);
    }

    /// <summary>One rejoin attempt against the last address (relay gets a fresh
    /// allocation each try; LAN redials). Runs async; Update() times it out.</summary>
    async void TryReconnect()
    {
        if (Manager == null || string.IsNullOrEmpty(Address)) { Fail("Could not reconnect to the match."); return; }
        reconnectBusy = true;
        reconnectAttemptStart = Time.unscaledTime;
        int attempt = ++reconnectAttemptId;
        Debug.Log("[net] reconnect attempt " + attempt + " to " + Address +
                  " t=" + reconnectAttemptStart.ToString("0.0"));

        bool started = false;
        try
        {
            if (AddressIsRelay)
            {
                if (!await EnsureServices()) { Debug.Log("[net] rejoin: services unavailable"); }
                else
                {
                    if (State != SessionState.Reconnecting || attempt != reconnectAttemptId) return;
                    var join = await RelayService.Instance.JoinAllocationAsync(Address.ToUpperInvariant());
                    if (State != SessionState.Reconnecting || attempt != reconnectAttemptId) return;
                    RelayServerEndpoint ep = PickEndpoint(join.ServerEndpoints, RelayConnectionType());
                    if (ep == null) Debug.Log("[net] rejoin: no usable endpoint.");
                    else
                    {
                        ApplyRelayData(ep, join.AllocationIdBytes, join.ConnectionData, join.HostConnectionData, join.Key);
                        leaving = false;
                        Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ConnectionPayload(localName, LocalGuid));
                        started = Manager.StartClient();
                        if (started) EnsureHandlers();
                    }
                }
            }
            else if (TryParseAddress(Address, out string ip, out ushort port))
            {
                Transport.UseWebSockets = false;
                Transport.SetConnectionData(ip, port);
                leaving = false;
                Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ConnectionPayload(localName, LocalGuid));
                started = Manager.StartClient();
                if (started) EnsureHandlers();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[net] reconnect attempt " + attempt + " failed: " + e.Message);
        }

        if (State != SessionState.Reconnecting || attempt != reconnectAttemptId) return;
        if (started) return;   // OnClientConnected (or the attempt timeout) resolves it
        Debug.LogWarning("[net] reconnect attempt " + attempt + " could not start; retrying");
        StopReconnectTransport();
        reconnectBusy = false;
        reconnectTimer = ReconnectRetrySeconds;
    }

    /// <summary>Retry the current outage immediately (reconnect overlay button).</summary>
    public void RetryNow()
    {
        if (State != SessionState.Reconnecting || reconnectBusy) return;
        reconnectTimer = 0f;
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
        matchLive = false;
        reconnectBusy = false;
        reconnectAttemptId++;

        if (Manager != null && Manager.IsListening) Manager.Shutdown();
        if (Manager != null) Destroy(Manager.gameObject);
        Manager = null; Transport = null;

        Players.Clear();
        namesByClient.Clear();
        guidByClient.Clear();
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

    private const string PlayerGuidKey = "td_player_guid";
    private string localGuid = "";

    /// <summary>Stable per-install identity, persisted like the player name.
    /// Sent in the connection payload so a mid-match rejoin maps to its seat.</summary>
    public string LocalGuid
    {
        get
        {
            if (string.IsNullOrEmpty(localGuid))
            {
                localGuid = PlayerPrefs.GetString(PlayerGuidKey, "");
                if (string.IsNullOrEmpty(localGuid))
                {
                    localGuid = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PlayerGuidKey, localGuid);
                    PlayerPrefs.Save();
                }
            }
            return localGuid;
        }
    }

    /// <summary>Connection approval payload: full version (scheme + build commit)
    /// on the first line, install guid on the second, display name after it. The
    /// host rejects any version mismatch, so builds from different commits never
    /// get into a lobby together.</summary>
    static string ConnectionPayload(string name, string guid)
    {
        return NetConfig.FullVersion + "\n" + guid + "\n" + name;
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
