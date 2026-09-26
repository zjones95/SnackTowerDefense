using System.Collections.Generic;
using UnityEngine;

// Multiplayer menu, join screen and lobby. Split into a partial class so the
// screens share TDGameManager's state machine (State, Style, ClearWorld, ...).
public partial class TDGameManager
{
    private enum MpScreen { Menu, Join, Connecting, Lobby }

    private const string PlayerNameKey = "td_player_name";

    private MpScreen mpScreen = MpScreen.Menu;
    private string mpName = "";
    private string mpJoinInput = "";
    private string mpError = "";
    private bool mpActive;          // local player is in a multiplayer match
    private int mpPlayerCount = 1;

    // match state (multiplayer only)
    private bool cleared;           // local board has cleared the current wave
    private bool eliminated;        // local board is out of lives
    private readonly List<RemoteBoard> remoteBoards = new List<RemoteBoard>();

    // spectating
    private Vector3 viewOffset;     // board the camera is currently on
    private int mySlot;             // this player's board slot
    private int viewSlot;           // slot being viewed
    private int slotCount = 1;

    public bool Cleared => cleared;
    public bool Eliminated => eliminated;
    public bool ViewingOwnBoard => !mpActive || viewSlot == mySlot;

    string SpectateName()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null && viewSlot >= 0 && viewSlot < ns.Players.Count)
            return ns.Players[viewSlot].Name;
        return "player";
    }

    void UpdateSpectate()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null) return;

        int count = Mathf.Max(1, slotCount);
        for (int i = 0; i < count && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SetViewSlot(i);

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.H))
            SetViewSlot(mySlot);
    }

    void SetViewSlot(int slot)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || slot < 0 || slot >= ns.Players.Count) return;

        viewSlot = slot;
        viewOffset = BoardLayout.Position(slot, Mathf.Max(1, slotCount));
        camFocus = viewOffset;
        SetSelected(null);
    }

    public void ApplyRemoteSnapshot(ulong boardId, BoardSnapshot snap)
    {
        for (int i = 0; i < remoteBoards.Count; i++)
        {
            if (remoteBoards[i] != null && remoteBoards[i].ClientId == boardId)
            {
                remoteBoards[i].Apply(snap);
                return;
            }
        }
    }

    // ------------------------------------------------------------------ flow
    void EnterMultiplayer()
    {
        NetworkSession ns = NetworkSession.Ensure();
        ns.MatchStarted -= OnMatchStarted;
        ns.MatchStarted += OnMatchStarted;

        mpName = PlayerPrefs.GetString(PlayerNameKey, "");
        if (string.IsNullOrEmpty(mpName)) mpName = "Player" + Random.Range(100, 999);
        mpJoinInput = "";
        mpError = "";
        mpActive = false;
        mpScreen = MpScreen.Menu;
        State = GameState.MultiplayerMenu;
        ClearWorld();
    }

    void LeaveMultiplayer()
    {
        if (NetworkSession.Instance != null)
        {
            NetworkSession.Instance.MatchStarted -= OnMatchStarted;
            NetworkSession.Instance.Leave();
        }
        mpActive = false;
        mpScreen = MpScreen.Menu;
        State = GameState.MainMenu;
        ClearWorld();
    }

    void OnMatchStarted()
    {
        mpActive = true;
        cleared = false;
        eliminated = false;
        SetSelected(null);

        NetworkSession ns = NetworkSession.Instance;
        int count = ns != null ? Mathf.Max(1, ns.PlayerCount) : 1;
        int slot = 0;
        if (ns != null)
        {
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == NetworkManagerLocalClientId()) { slot = i; break; }
        }

        mpPlayerCount = count;
        slotCount = count;
        mySlot = slot;
        viewSlot = slot;
        CurrentDifficulty = ns != null ? ns.MatchDifficulty : Difficulty.Normal;

        StartRun(BoardLayout.Position(slot, count));
        BuildRemoteBoards(ns, slot, count);
        MatchSync.Ensure().BeginMatch();
        SpectateSync.Ensure().Begin();
        message = "Multiplayer: " + count + " player(s)";
        messageTimer = 2.5f;
    }

    void BuildRemoteBoards(NetworkSession ns, int mySlot, int count)
    {
        remoteBoards.Clear();
        if (ns == null || worldRoot == null) return;
        for (int i = 0; i < ns.Players.Count; i++)
        {
            if (i == mySlot) continue;
            RemoteBoard rb = RemoteBoard.Create(worldRoot, BoardLayout.Position(i, count),
                ns.Players[i].ClientId, ns.Players[i].Name);
            remoteBoards.Add(rb);
        }
    }

    // ------------------------------------------------------------- match flow
    /// <summary>Called when the match moves to a (new) wave.</summary>
    public void MatchWaveStart(int wave, float prepSeconds)
    {
        if (!mpActive) return;
        Wave = wave;
        cleared = false;
        Round = RoundState.Preparing;
        prepTimer = prepSeconds;
    }

    /// <summary>Called when the match says the wave is live.</summary>
    public void BeginWaveFromMatch()
    {
        if (!mpActive || eliminated) return;
        cleared = false;
        BeginWave();
    }

    public void OnMatchOver(bool victory)
    {
        if (!mpActive) return;
        State = victory ? GameState.Victory : GameState.GameOver;
    }

    void EliminateLocal()
    {
        eliminated = true;
        cleared = true;   // an out board no longer holds up the wave
        Round = RoundState.Preparing;
        prepTimer = 0f;

        for (int i = Mobs.Count - 1; i >= 0; i--)
            if (Mobs[i] != null) Destroy(Mobs[i].gameObject);
        Mobs.Clear();
        spawnQueue.Clear();

        message = "Your board is out - waiting for the others";
        messageTimer = 3f;
        if (MatchSync.Instance != null)
            MatchSync.Instance.ReportLocal(Lives, Money, Wave, true, true);
    }

    void UpdateRemoteBoards()
    {
        if (MatchSync.Instance == null) return;
        for (int i = 0; i < remoteBoards.Count; i++)
        {
            RemoteBoard rb = remoteBoards[i];
            if (rb == null) continue;

            MatchSync.BoardState b = MatchSync.Instance.BoardFor(rb.ClientId);
            if (b == null) { rb.SetStatus("disconnected"); continue; }

            string phase = b.Eliminated ? "out" : (b.Cleared ? "cleared" : "wave " + b.Wave);
            rb.SetStatus(phase + "   |   " + b.Lives + " lives");
        }
    }

    void ReturnToLobby()
    {
        ClearWorld();
        remoteBoards.Clear();
        mpActive = false;
        cleared = false;
        eliminated = false;
        viewSlot = mySlot;
        viewOffset = Vector3.zero;
        mpScreen = MpScreen.Lobby;
        State = GameState.MultiplayerMenu;
    }

    void DrawScoreboard()
    {
        MatchSync ms = MatchSync.Instance;
        if (ms == null) return;

        float y = Screen.height * 0.47f;
        GUI.Label(new Rect(0, y - 26f, Screen.width, 24f), "Scoreboard",
            Style(18, TextAnchor.MiddleCenter, new Color(0.9f, 0.92f, 1f)));

        for (int i = 0; i < ms.Boards.Count; i++)
        {
            MatchSync.BoardState b = ms.Boards[i];
            string name = string.IsNullOrEmpty(b.Name) ? ("Player " + b.ClientId) : b.Name;
            string line = name.PadRight(18) + "wave " + b.Wave + "    " + b.Lives + " lives    $" + b.Money +
                          (b.Eliminated ? "    OUT" : "");
            GUI.Label(new Rect(Screen.width * 0.5f - 280f, y + i * 22f, 560f, 20f), line,
                Style(15, TextAnchor.MiddleLeft, b.Eliminated ? new Color(0.8f, 0.65f, 0.65f) : Color.white));
        }
    }

    void MpBack()
    {
        switch (mpScreen)
        {
            case MpScreen.Menu:
                LeaveMultiplayer();
                break;
            case MpScreen.Join:
                mpError = "";
                mpScreen = MpScreen.Menu;
                break;
            case MpScreen.Connecting:
                if (NetworkSession.Instance != null) NetworkSession.Instance.Leave();
                mpError = "";
                mpScreen = MpScreen.Menu;
                break;
            case MpScreen.Lobby:
                LeaveMultiplayer();
                break;
        }
    }

    void PersistName()
    {
        mpName = (mpName ?? "").Trim();
        if (string.IsNullOrEmpty(mpName)) mpName = "Player";
        PlayerPrefs.SetString(PlayerNameKey, mpName);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------- UI
    void DrawMultiplayer()
    {
        NetworkSession ns = NetworkSession.Instance;

        Color old = GUI.color;
        GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0, Screen.height * 0.09f, Screen.width, 60), "MULTIPLAYER",
            Style(44, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        // A session that failed drops us back to the menu screen with an error.
        if (ns != null && ns.State == NetworkSession.SessionState.Failed)
        {
            mpError = ns.Error;
            mpScreen = MpScreen.Menu;
        }

        switch (mpScreen)
        {
            case MpScreen.Menu: DrawMpMenu(ns); break;
            case MpScreen.Join: DrawMpJoin(ns); break;
            case MpScreen.Connecting: DrawMpConnecting(ns); break;
            case MpScreen.Lobby: DrawMpLobby(ns); break;
        }

        if (!string.IsNullOrEmpty(mpError))
            GUI.Label(new Rect(0, Screen.height - 78f, Screen.width, 28), mpError,
                Style(16, TextAnchor.MiddleCenter, new Color(1f, 0.52f, 0.46f)));

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            MpBack();
    }

    void DrawMpMenu(NetworkSession ns)
    {
        float cx = Screen.width * 0.5f;
        float bw = 320f, bh = 54f;
        float bx = cx - bw * 0.5f;
        float by = Screen.height * 0.34f;

        GUI.Label(new Rect(cx - 200f, by - 52f, 400f, 24f), "Your name:",
            Style(16, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 0.9f)));
        mpName = GUI.TextField(new Rect(cx - 150f, by - 28f, 300f, 32f), mpName, 24);

        bool busy = ns != null && (ns.State == NetworkSession.SessionState.Hosting ||
                                   ns.State == NetworkSession.SessionState.Connecting);
        GUI.enabled = !busy;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Host Game"))
        {
            Click();
            PersistName();
            mpError = "";
            NetworkSession.Ensure().HostGame(mpName);
            mpScreen = MpScreen.Lobby;
        }
        if (GUI.Button(new Rect(bx, by + bh + 14f, bw, bh), "Join Game"))
        {
            Click();
            PersistName();
            mpError = "";
            mpJoinInput = "";
            mpScreen = MpScreen.Join;
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(bx, by + 2f * (bh + 14f), bw, bh), "Back"))
        {
            Click();
            LeaveMultiplayer();
        }

        GUI.Label(new Rect(cx - 260f, by + 3f * (bh + 14f) + 10f, 520f, 40f),
            "Hosting needs an internet connection for a join code; without it the\ngame falls back to a LAN address (same network only).",
            Style(13, TextAnchor.UpperCenter, new Color(0.65f, 0.68f, 0.72f)));
    }

    void DrawMpJoin(NetworkSession ns)
    {
        float cx = Screen.width * 0.5f;
        float bw = 320f, bh = 54f;
        float bx = cx - bw * 0.5f;
        float by = Screen.height * 0.40f;

        GUI.Label(new Rect(cx - 260f, by - 60f, 520f, 24f),
            "Enter the host's join code (or an ip address):",
            Style(17, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 0.95f)));
        mpJoinInput = GUI.TextField(new Rect(cx - 170f, by - 32f, 340f, 34f), mpJoinInput, 32);

        GUI.enabled = !string.IsNullOrEmpty((mpJoinInput ?? "").Trim());
        if (GUI.Button(new Rect(bx, by + 18f, bw, bh), "Join"))
        {
            Click();
            PersistName();
            mpError = "";
            NetworkSession.Ensure().JoinGame(mpJoinInput, mpName);
            mpScreen = MpScreen.Connecting;
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(bx, by + 18f + bh + 14f, bw, bh), "Back"))
        {
            Click();
            mpScreen = MpScreen.Menu;
        }
    }

    void DrawMpConnecting(NetworkSession ns)
    {
        if (ns == null) { mpScreen = MpScreen.Menu; return; }

        // Connected -- move into the lobby screen.
        if (ns.InLobby) { mpScreen = MpScreen.Lobby; return; }

        float cx = Screen.width * 0.5f;
        GUI.Label(new Rect(0, Screen.height * 0.40f, Screen.width, 30f),
            "Connecting to " + (mpJoinInput ?? "") + " ...",
            Style(22, TextAnchor.MiddleCenter, Color.white));

        GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 24f),
            Mathf.FloorToInt(ns.ConnectingSeconds) + "s   (gives up after 12s)",
            Style(14, TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.76f)));

        if (GUI.Button(new Rect(cx - 110f, Screen.height * 0.52f, 220f, 48f), "Cancel"))
        {
            Click();
            ns.Leave();
            mpScreen = MpScreen.Menu;
        }
    }

    void DrawMpLobby(NetworkSession ns)
    {
        float cx = Screen.width * 0.5f;

        if (ns == null)
        {
            mpScreen = MpScreen.Menu;
            return;
        }

        if (ns.State == NetworkSession.SessionState.Connecting ||
            (ns.State == NetworkSession.SessionState.Hosting && string.IsNullOrEmpty(ns.Address)))
        {
            GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 30f), "Setting up the lobby...",
                Style(20, TextAnchor.MiddleCenter, Color.white));
            return;
        }

        bool host = ns.IsHost;

        GUI.Label(new Rect(0, Screen.height * 0.20f, Screen.width, 30f),
            host ? "You are the HOST" : "Waiting for the host to start...",
            Style(20, TextAnchor.MiddleCenter, host ? new Color(1f, 0.9f, 0.5f) : new Color(0.8f, 0.9f, 1f)));

        // ---- share info -------------------------------------------------
        string label = ns.AddressIsRelay ? "Join code" : "LAN address";
        GUI.Label(new Rect(cx - 240f, Screen.height * 0.27f, 480f, 26f),
            label + ":", Style(17, TextAnchor.MiddleCenter, new Color(0.75f, 0.8f, 0.85f)));

        GUI.Label(new Rect(cx - 240f, Screen.height * 0.30f, 480f, 40f),
            string.IsNullOrEmpty(ns.Address) ? "-" : ns.Address,
            Style(30, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.6f)));

        if (!string.IsNullOrEmpty(ns.Address) &&
            GUI.Button(new Rect(cx - 70f, Screen.height * 0.355f, 140f, 28f), "Copy"))
        {
            Click();
            GUIUtility.systemCopyBuffer = ns.Address;
        }
        if (!ns.AddressIsRelay && !string.IsNullOrEmpty(ns.Address))
            GUI.Label(new Rect(cx - 240f, Screen.height * 0.39f, 480f, 22f),
                "(same machine? use 127.0.0.1:" + NetConfig.DefaultPort + ")",
                Style(13, TextAnchor.MiddleCenter, new Color(0.65f, 0.68f, 0.72f)));

        // ---- player list ------------------------------------------------
        int count = ns.PlayerCount;
        mpPlayerCount = Mathf.Max(1, count);
        float listY = Screen.height * 0.46f;
        GUI.Label(new Rect(cx - 200f, listY, 400f, 26f),
            "Players (" + count + " / " + NetConfig.MaxPlayers + ")",
            Style(19, TextAnchor.MiddleCenter, Color.white));

        var players = ns.Players;
        for (int i = 0; i < players.Count; i++)
        {
            string nm = players[i].Name;
            if (players[i].IsHost) nm += "  (host)";
            if (players[i].ClientId == NetworkManagerLocalClientId()) nm += "   (you)";
            GUI.Label(new Rect(cx - 200f, listY + 30f + i * 24f, 400f, 22f),
                "• " + nm, Style(16, TextAnchor.MiddleLeft, new Color(0.9f, 0.92f, 0.95f)));
        }

        // ---- difficulty ----
        float dy = Screen.height * 0.63f;
        GUI.Label(new Rect(cx - 280f, dy, 560f, 24f),
            host ? "Difficulty (you choose):" : "Difficulty:",
            Style(16, TextAnchor.MiddleCenter, new Color(0.75f, 0.8f, 0.85f)));

        if (host)
        {
            float dbw = 116f, dbh = 34f, gap = 10f;
            float total = 4f * dbw + 3f * gap;
            float dx = cx - total * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                Difficulty d = (Difficulty)i;
                bool sel = (int)ns.MatchDifficulty == i;
                GUIStyle st = new GUIStyle(GUI.skin.button);
                st.fontSize = 14;
                st.normal.textColor = sel ? Color.white : TDBalance.DifficultyColour(d);
                if (GUI.Button(new Rect(dx + i * (dbw + gap), dy + 26f, dbw, dbh),
                        (sel ? "> " : "") + TDBalance.DifficultyName(d), st))
                {
                    Click();
                    ns.SetDifficulty(d);
                }
            }
        }
        else
        {
            GUI.Label(new Rect(cx - 240f, dy + 26f, 480f, 28f),
                TDBalance.DifficultyName(ns.MatchDifficulty) + "   (" + TDBalance.DifficultyBlurb(ns.MatchDifficulty) + ")",
                Style(16, TextAnchor.MiddleCenter, TDBalance.DifficultyColour(ns.MatchDifficulty)));
        }

        // ---- actions ----------------------------------------------------
        float ay = Screen.height - 150f;
        if (host)
        {
            GUI.enabled = count >= 1;
            if (GUI.Button(new Rect(cx - 150f, ay, 300f, 52f), "Start Game"))
            {
                Click();
                ns.StartMatch();
            }
            GUI.enabled = true;
            GUI.Label(new Rect(cx - 260f, ay + 56f, 520f, 22f),
                "Up to " + NetConfig.MaxPlayers + " players. You can start at any time.",
                Style(13, TextAnchor.MiddleCenter, new Color(0.65f, 0.68f, 0.72f)));
        }

        if (GUI.Button(new Rect(cx - 100f, ay + 88f, 200f, 40f), "Leave"))
        {
            Click();
            LeaveMultiplayer();
        }
    }

    // The local client id, without importing Unity.Netcode into this file.
    static ulong NetworkManagerLocalClientId()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null ? nm.LocalClientId : ulong.MaxValue;
    }

    void Click()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
    }
}
