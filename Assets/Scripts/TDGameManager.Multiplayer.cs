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
    private bool failedRouted;   // Failed already routed to Menu/Join (runs once)
    private bool mpActive;          // local player is in a multiplayer match
    private int mpPlayerCount = 1;
    private bool mpScoreboardCollapsed = true;   // top-right scoreboard tab state

    // match state (multiplayer only)
    private bool cleared;           // local board has cleared the current wave
    private bool eliminated;        // local board is out of lives
    private bool mpEndDismissed;    // MP end screen dismissed to keep spectating
    private readonly List<RemoteBoard> remoteBoards = new List<RemoteBoard>();
    // Set when the link returns mid-match: remote boards are rebuilt once the
    // fresh roster (with our new ClientId) arrives via the lobby message.
    private bool remoteBoardsDirty;

    // spectating
    private Vector3 viewOffset;     // board the camera is currently on
    private int mySlot;             // this player's board slot
    private int viewSlot;           // slot being viewed
    private int slotCount = 1;

    public bool Cleared => cleared;
    public bool Eliminated => eliminated;
    public bool ViewingOwnBoard => !mpActive || viewSlot == mySlot;
    public bool Paused => paused;
    public bool SpectatingAfterResult => mpActive && mpEndDismissed &&
        (State == GameState.GameOver || State == GameState.Victory);

    /// <summary>Total money invested in every tower still on this board. Relayed
    /// as the scoreboard's Tower Value column (see MatchSync.BoardState).</summary>
    public int LocalTowerValue()
    {
        int total = 0;
        foreach (Tower t in AllTowers)
            if (t != null) total += t.InvestedCost;
        return total;
    }

    /// <summary>The remote board shown on a spectate slot, if one exists for it.</summary>
    RemoteBoard BoardForSlot(int slot)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || slot < 0 || slot >= ns.Players.Count) return null;
        ulong id = ns.Players[slot].ClientId;
        for (int i = 0; i < remoteBoards.Count; i++)
            if (remoteBoards[i] != null && remoteBoards[i].ClientId == id) return remoteBoards[i];
        return null;
    }

    string SpectateName()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null && viewSlot >= 0 && viewSlot < ns.Players.Count)
            return ns.Players[viewSlot].Name;
        return "player";
    }

    /// <summary>Client id of the board the camera is on (for its board name).</summary>
    ulong CurrentViewClientId()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null && viewSlot >= 0 && viewSlot < ns.Players.Count)
            return ns.Players[viewSlot].ClientId;
        return ulong.MaxValue;
    }

    void UpdateSpectate()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null) return;
        if (ChatSync.IsTyping) return;   // digits/H are chat text right now

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

        if (slot != viewSlot && TDAudio.Instance != null) TDAudio.Instance.StopBoardSounds();
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

    /// <summary>Replays another board's cosmetic FX (splash bursts, tracer bolts)
    /// on its RemoteBoard. Events are board-local; the board adds its offset.</summary>
    public void ApplyRemoteFx(ulong boardId, List<FxEvent> events)
    {
        for (int i = 0; i < remoteBoards.Count; i++)
        {
            if (remoteBoards[i] != null && remoteBoards[i].ClientId == boardId)
            {
                remoteBoards[i].ReplayFx(events);
                return;
            }
        }
    }

    // ------------------------------------------------------------------ flow
    void EnterMultiplayer()
    {
        // Tropical Island's large route is a solo prototype; independent boards
        // in multiplayer still use the classic layout.
        if (ActiveTheme == BoardTheme.TropicalIsland) SetTheme(BoardTheme.Bedroom);
        NetworkSession ns = NetworkSession.Ensure();
        ns.MatchStarted -= OnMatchStarted;
        ns.MatchStarted += OnMatchStarted;
        ns.Reconnected -= OnReconnected;
        ns.Reconnected += OnReconnected;

        if (ChatSync.Instance != null) ChatSync.Instance.Close();
        mpScoreboardCollapsed = true;
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
        if (TDAudio.Instance != null) TDAudio.Instance.StopBoardSounds();
        if (NetworkSession.Instance != null)
        {
            NetworkSession.Instance.MatchStarted -= OnMatchStarted;
            NetworkSession.Instance.Reconnected -= OnReconnected;
            NetworkSession.Instance.Leave();
        }
        if (ChatSync.Instance != null) ChatSync.Instance.Close();
        mpActive = false;
        mpScoreboardCollapsed = true;
        mpScreen = MpScreen.Menu;
        settingsOpen = false;
        RestoreTimeScale();
        State = GameState.MainMenu;
        ClearWorld();
    }

    void OnMatchStarted()
    {
        mpActive = true;
        cleared = false;
        eliminated = false;
        mpEndDismissed = false;
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
        ChatSync.Ensure().Begin();
        FxSync.Ensure().Begin();
        BoardAudioSync.Ensure().Begin();
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
                ns.Players[i].ClientId, ns.Players[i].Name, ns.Players[i].Theme);
            remoteBoards.Add(rb);
        }
    }

    /// <summary>Board choice a player picked in the lobby, by client id (0 when
    /// unknown). Used when a board is adopted after a rejoin.</summary>
    int ThemeForClient(ulong id)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null)
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == id) return ns.Players[i].Theme;
        return 0;
    }

    /// <summary>Mid-match link return: the board kept simulating through the
    /// outage, so nothing resets. Remote boards rebuild once the fresh roster
    /// arrives, and the live local row is published for the host to adopt.</summary>
    void OnReconnected()
    {
        if (!mpActive) return;
        remoteBoardsDirty = true;
        if (MatchSync.Instance != null) MatchSync.Instance.RejoinSync();
        message = "Reconnected - welcome back!";
        messageTimer = 3f;
        if (ChatSync.Instance != null) ChatSync.Instance.SendSystem("Reconnected.");
    }

    /// <summary>Reassigns RemoteBoards after an id change (a rejoin carries a
    /// fresh ClientId). Matches rows to boards by name, preferring boards that
    /// lost their row ("disconnected"), so snapshots resume on the right board
    /// and nobody's slot shifts.</summary>
    void SyncRemoteBoards()
    {
        MatchSync ms = MatchSync.Instance;
        NetworkSession ns = NetworkSession.Instance;
        if (ms == null || ns == null) return;

        ulong me = NetworkSession.LocalClientId;
        foreach (MatchSync.BoardState b in ms.Boards)
        {
            if (b.ClientId == me) continue;
            bool has = false;
            for (int i = 0; i < remoteBoards.Count; i++)
                if (remoteBoards[i] != null && remoteBoards[i].ClientId == b.ClientId) { has = true; break; }
            if (has) continue;

            // Adopt an orphaned board (one whose row vanished), same name first.
            RemoteBoard orphan = null, anyOrphan = null;
            for (int i = 0; i < remoteBoards.Count; i++)
            {
                RemoteBoard rb = remoteBoards[i];
                if (rb == null || ms.BoardFor(rb.ClientId) != null) continue;
                if (anyOrphan == null) anyOrphan = rb;
                if (rb.PlayerName == b.Name) { orphan = rb; break; }
            }
            RemoteBoard target = orphan ?? anyOrphan;
            if (target != null) target.Reassign(b.ClientId, b.Name, ThemeForClient(b.ClientId));
        }

        // Rebuild once after our own rejoin lands a fresh roster with our new id.
        if (remoteBoardsDirty)
        {
            bool haveSelf = false;
            int slot = mySlot;
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == me) { haveSelf = true; slot = i; break; }
            if (haveSelf)
            {
                remoteBoardsDirty = false;
                if (TDAudio.Instance != null) TDAudio.Instance.StopBoardSounds();
                mySlot = slot;
                viewSlot = slot;
                slotCount = Mathf.Max(1, ns.PlayerCount);
                mpPlayerCount = slotCount;
                BuildRemoteBoards(ns, slot, slotCount);
                SetViewSlot(slot);
            }
        }
    }

    // ------------------------------------------------------------- match flow
    /// <summary>Called when this board moves to a (new) wave. Waves are
    /// independent per peer, so this is driven by our own MatchSync, not a
    /// shared host clock.</summary>
    public void MatchWaveStart(int wave, float prepSeconds)
    {
        if (!mpActive) return;
        Wave = wave;
        cleared = false;
        Round = RoundState.Preparing;
        prepTimer = prepSeconds;
    }

    /// <summary>Called after the local prep when our wave goes live.</summary>
    public void BeginWaveFromMatch()
    {
        if (!mpActive || eliminated) return;
        cleared = false;
        BeginWave();
    }

    public void OnMatchOver(bool victory)
    {
        if (!mpActive) return;
        // Per-player results: the first result sticks (each board finishes once).
        if (State == GameState.Victory || State == GameState.GameOver) return;
        if (ChatSync.Instance != null) ChatSync.Instance.Close();
        RestoreTimeScale();
        State = victory ? GameState.Victory : GameState.GameOver;
    }

    void EliminateLocal()
    {
        eliminated = true;
        cleared = true;   // stops this board; it no longer matters to any clock
        Round = RoundState.Preparing;
        prepTimer = 0f;

        for (int i = Mobs.Count - 1; i >= 0; i--)
            if (Mobs[i] != null) Destroy(Mobs[i].gameObject);
        Mobs.Clear();
        spawnQueue.Clear();

        message = "Your board is out - spectating";
        messageTimer = 3f;
        if (MatchSync.Instance != null)
            MatchSync.Instance.ReportLocal(Lives, Money, Wave, true, true);
        // Per-player defeat: own GameOver screen now (dismissable to spectate).
        OnMatchOver(false);
    }

    void UpdateRemoteBoards()
    {
        if (MatchSync.Instance == null) return;
        SyncRemoteBoards();
        for (int i = 0; i < remoteBoards.Count; i++)
        {
            RemoteBoard rb = remoteBoards[i];
            if (rb == null) continue;

            MatchSync.BoardState b = MatchSync.Instance.BoardFor(rb.ClientId);
            if (b == null) { rb.SetStatus("disconnected"); continue; }

            string phase = b.Absent ? "reconnecting..."
                : b.Eliminated ? "out"
                : (b.Cleared ? "cleared" : "wave " + b.Wave);
            rb.SetStatus(phase + "   |   " + b.Lives + " lives");
        }
    }

    void ReturnToLobby()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.StopBoardSounds();
        ClearWorld();
        remoteBoards.Clear();
        mpActive = false;
        cleared = false;
        eliminated = false;
        mpEndDismissed = false;
        viewSlot = mySlot;
        viewOffset = Vector3.zero;
        mpScoreboardCollapsed = true;
        if (ChatSync.Instance != null) ChatSync.Instance.Close();
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
            float w = Mathf.Min(700f, Screen.width - 24f);
            float x = (Screen.width - w) * 0.5f;
            GUIStyle st = Style(15, TextAnchor.MiddleLeft,
                b.Eliminated ? new Color(0.8f, 0.65f, 0.65f) : Color.white);
            st.wordWrap = false;
            st.clipping = TextClipping.Clip;
            string stats = "wave " + b.Wave + "    " + b.Lives + " lives    $" + b.Money +
                           (b.Eliminated ? "    OUT" : "");
            float statsW = Mathf.Min(w * .60f, st.CalcSize(new GUIContent(stats)).x + 4f);
            float nameW = Mathf.Max(0f, w - statsW - 8f);
            GUI.Label(new Rect(x, y + i * 22f, nameW, 20f), FitScoreName(name, st, nameW), st);
            GUI.Label(new Rect(x + nameW + 8f, y + i * 22f, statsW, 20f), stats, st);
        }
    }

    /// <summary>Remote board SFX only on the currently viewed slot.</summary>
    public bool HearingRemoteBoard(ulong boardId)
    {
        return mpActive && !ViewingOwnBoard && CurrentViewClientId() == boardId;
    }

    /// <summary>Keep one-line names within their column, even with status badges.</summary>
    static string FitScoreName(string name, GUIStyle style, float width)
    {
        if (width <= 0f) return "";
        GUIContent text = new GUIContent(name);
        if (style.CalcSize(text).x <= width) return name;
        const string dots = "...";
        text.text = dots;
        if (style.CalcSize(text).x > width) return "";
        int lo = 0, hi = name.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            text.text = name.Substring(0, mid) + dots;
            if (style.CalcSize(text).x <= width) lo = mid;
            else hi = mid - 1;
        }
        return name.Substring(0, lo) + dots;
    }

    // ------------------------------------------------- in-match MP overlays
    /// <summary>Draws the live multiplayer overlays. Called from ChatSync's
    /// OnGUI so all in-match IMGUI stays in this partial class (the game's own
    /// OnGUI is in TDGameManager.cs and only draws the HUD/end screens).</summary>
    public void DrawMpOverlays()
    {
        if (!mpActive) return;
        DrawMpScoreboard();
        DrawMpChat();
        DrawMpReconnect();
    }

    /// <summary>Mid-match link outage: the board keeps simulating underneath,
    /// so this is a status overlay, not a dead end. Auto-retry runs in the
    /// background; the player can force a retry now or abandon the match.</summary>
    void DrawMpReconnect()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || (!ns.Reconnecting && ns.State != NetworkSession.SessionState.Failed)) return;
        if (State != GameState.Playing || paused) return;

        bool failed = ns.State == NetworkSession.SessionState.Failed;

        float w = 460f, h = 190f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.30f;

        Color old = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.14f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(x, y + 12f, w, 34f), failed ? "RECONNECTION FAILED" : "CONNECTION LOST",
            Style(26, TextAnchor.MiddleCenter, new Color(1f, 0.62f, 0.4f)));
        GUI.Label(new Rect(x + 20f, y + 52f, w - 40f, 44f),
            failed ? ns.Error : "Retrying (" + Mathf.FloorToInt(ns.ReconnectingSeconds) + "s) - your game continues underneath.",
            Style(14, TextAnchor.MiddleCenter, new Color(0.88f, 0.9f, 0.94f)));

        GUIStyle btn = PaperButton(18);
        if (!failed && GUI.Button(new Rect(x + 30f, y + 108f, 180f, 44f), "Retry now", btn))
        {
            Click();
            ns.RetryNow();
        }
        if (GUI.Button(new Rect(x + w - 210f, y + 108f, 180f, 44f), "Leave match", btn))
        {
            Click();
            LeaveMultiplayer();
        }
    }

    /// <summary>Collapsible scoreboard docked top-right: a small tab when
    /// collapsed, the full per-board roster when expanded. Sits below the
    /// top-right stats and clear of the centred boss bar (y ~ 44).</summary>
    void DrawMpScoreboard()
    {
        if (State != GameState.Playing || paused) return;
        MatchSync ms = MatchSync.Instance;
        if (ms == null) return;

        float w = Mathf.Min(460f, Screen.width - 16f);
        const float tabW = 96f;
        float x0 = Screen.width - w - 8f;
        float y = 112f;

        if (mpScoreboardCollapsed)
        {
            if (GUI.Button(new Rect(x0 + w - tabW, y, tabW, 24f), "Scores >", PaperButton(14)))
            {
                Click();
                mpScoreboardCollapsed = false;
            }
            return;
        }

        int rows = ms.Boards.Count;
        float panelH = 50f + rows * 20f;

        Color old = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.14f, 0.88f);
        GUI.DrawTexture(new Rect(x0 - 6f, y - 8f, w + 12f, panelH + 12f), Texture2D.whiteTexture);
        GUI.color = old;

        if (GUI.Button(new Rect(x0 + w - tabW, y, tabW, 24f), "Scores v", PaperButton(14)))
        {
            Click();
            mpScoreboardCollapsed = true;
            return;
        }

        GUIStyle hdr = Style(11, TextAnchor.MiddleLeft, new Color(0.62f, 0.68f, 0.76f));
        float nameW = Mathf.Max(0f, w - 245f);
        float waveX = x0 + w - 239f, livesX = x0 + w - 203f;
        float goldX = x0 + w - 155f, towerX = x0 + w - 80f;
        GUI.Label(new Rect(x0 + 6f, y + 4f, nameW, 18f), "Name", hdr);
        GUI.Label(new Rect(waveX, y + 4f, 32f, 18f), "Wv", hdr);
        GUI.Label(new Rect(livesX, y + 4f, 44f, 18f), "Lives", hdr);
        GUI.Label(new Rect(goldX, y + 4f, 70f, 18f), "Gold", hdr);
        GUI.Label(new Rect(towerX, y + 4f, 74f, 18f), "Tower", hdr);

        ulong me = NetworkSession.LocalClientId;
        for (int i = 0; i < rows; i++)
        {
            MatchSync.BoardState b = ms.Boards[i];
            float ry = y + 26f + i * 20f;
            bool mine = b.ClientId == me;
            string nm = string.IsNullOrEmpty(b.Name) ? ("Player " + b.ClientId) : b.Name;
            if (mine) nm += " (you)";
            if (b.Eliminated) nm = "OUT " + nm;
            else if (b.Absent) nm = "... " + nm;

            Color c = b.Eliminated ? new Color(0.8f, 0.62f, 0.62f)
                    : b.Absent ? new Color(0.65f, 0.68f, 0.72f)
                    : mine ? new Color(1f, 0.9f, 0.5f)
                    : new Color(0.88f, 0.92f, 1f);
            GUIStyle st = Style(12, TextAnchor.MiddleLeft, c);
            st.wordWrap = false;
            st.clipping = TextClipping.Clip;
            GUI.Label(new Rect(x0 + 6f, ry, nameW, 18f), FitScoreName(nm, st, nameW), st);
            GUI.Label(new Rect(waveX, ry, 32f, 18f), b.Wave.ToString(), st);
            GUI.Label(new Rect(livesX, ry, 44f, 18f), b.Lives.ToString(), st);
            GUI.Label(new Rect(goldX, ry, 70f, 18f), "$" + b.GoldGenerated, st);
            GUI.Label(new Rect(towerX, ry, 74f, 18f), "$" + b.TowerValue, st);
        }

        // Join code, so a dropped player can re-enter it (auto-rejoin normally
        // beats them to it, but the code is here if they need it).
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null && !string.IsNullOrEmpty(ns.Address))
        {
            GUIStyle code = Style(12, TextAnchor.MiddleLeft, new Color(0.6f, 0.85f, 0.65f));
            code.wordWrap = false;
            code.clipping = TextClipping.Clip;
            GUI.Label(new Rect(x0 + 6f, y + 30f + rows * 20f, w - 12f, 18f),
                FitScoreName("Code: " + ns.Address, code, w - 12f), code);
        }
    }

    /// <summary>Lower-left chat log and input line (issue #27). Shows the last
    /// few lines whenever a match is live; the input row appears while typing.</summary>
    void DrawMpChat()
    {
        if ((State != GameState.Playing && !SpectatingAfterResult) || paused) return;
        ChatSync chat = ChatSync.Instance;
        if (chat == null) return;

        const float x = 10f;
        const float w = 372f;
        const float lineH = 17f;

        int count = Mathf.Min(ChatSync.VisibleLines, chat.Lines.Count);
        if (count == 0 && !chat.Open) return;

        float inputBottom = Screen.height - 74f;   // just above the toolbar
        float logBottom = chat.Open ? inputBottom - 24f : inputBottom;
        float logTop = logBottom - count * lineH;

        float top = (chat.Open ? logTop - 18f : logTop) - 5f;
        Color old = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.14f, 0.85f);
        GUI.DrawTexture(new Rect(x - 6f, top, w + 12f, (inputBottom + 5f) - top), Texture2D.whiteTexture);
        GUI.color = old;

        int first = chat.Lines.Count - count;
        for (int i = 0; i < count; i++)
        {
            ChatSync.Line ln = chat.Lines[first + i];
            float ly = logTop + i * lineH;
            if (ln.System)
                GUI.Label(new Rect(x, ly, w, lineH), ln.Text,
                    Style(12, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.4f)));
            else
                GUI.Label(new Rect(x, ly, w, lineH), "<" + ln.Name + "> " + ln.Text,
                    Style(12, TextAnchor.MiddleLeft, new Color(0.9f, 0.95f, 1f)));
        }

        if (chat.Open)
        {
            GUI.Label(new Rect(x, logTop - 17f, w, 16f), "Enter: send    Esc: cancel",
                Style(11, TextAnchor.MiddleLeft, new Color(0.72f, 0.75f, 0.8f)));
            GUI.Label(new Rect(x, inputBottom - 20f, w, 20f), "Say: " + chat.Buffer + "_",
                Style(13, TextAnchor.MiddleLeft, new Color(0.7f, 1f, 0.7f)));
        }
        else
        {
            GUI.Label(new Rect(x, inputBottom - 16f, w, 16f), "T: chat",
                Style(11, TextAnchor.MiddleLeft, new Color(0.7f, 0.73f, 0.78f)));
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

    /// <summary>Lobby board choice: swaps this peer's own theme and tells the
    /// host so the roster (and every remote board) follows. Other players'
    /// boards are untouched.</summary>
    void SelectLocalTheme(int dir)
    {
        BoardTheme next = dir >= 0 ? TDBoardBuilder.NextTheme(ActiveTheme)
                                   : TDBoardBuilder.PrevTheme(ActiveTheme);
        if (next == BoardTheme.TropicalIsland)
            next = dir >= 0 ? TDBoardBuilder.NextTheme(next) : TDBoardBuilder.PrevTheme(next);
        SetTheme(next);                                            // local board + lighting + persistence
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null && ns.InSession) ns.SetTheme(TDBoardBuilder.ThemeIndex(next));
    }

    /// <summary>Board choice of another player, for display.</summary>
    string ThemeNameFor(ulong clientId)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null)
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == clientId)
                    return TDBoardBuilder.ThemeName(TDBoardBuilder.ClampTheme(ns.Players[i].Theme));
        return "-";
    }

    // ------------------------------------------------------------------- UI
    void DrawMultiplayer()
    {
        NetworkSession ns = NetworkSession.Instance;

        Color old = GUI.color;
        GUI.color = new Color(0.10f, 0.11f, 0.12f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(0, Screen.height * 0.09f, Screen.width, 60), "MULTIPLAYER",
            Style(44, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));

        // A session that failed drops us back with an error. If we were in a
        // match (host died or grace ran out), land on Join with the last
        // address prefilled so rejoining is one click.
        if (ns != null && ns.State == NetworkSession.SessionState.Failed)
        {
            if (!failedRouted)
            {
                failedRouted = true;
                mpError = ns.Error;
                if (!string.IsNullOrEmpty(ns.Address))
                {
                    mpJoinInput = ns.Address;
                    mpScreen = MpScreen.Join;
                }
                else mpScreen = MpScreen.Menu;
            }
        }
        else failedRouted = false;

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

        // Build version (and commit) so players can check they match before joining.
        GUI.Label(new Rect(10f, Screen.height - 24f, 420f, 20f), "v" + NetConfig.FullVersion,
            Style(12, TextAnchor.LowerLeft, new Color(0.55f, 0.58f, 0.62f)));

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

        GUIStyle btn = PaperButton(22);
        if (GUI.Button(new Rect(bx, by, bw, bh), "Host Game", btn))
        {
            Click();
            PersistName();
            mpError = "";
            NetworkSession.Ensure().HostGame(mpName);
            mpScreen = MpScreen.Lobby;
        }
        if (GUI.Button(new Rect(bx, by + bh + 14f, bw, bh), "Join Game", btn))
        {
            Click();
            PersistName();
            mpError = "";
            mpJoinInput = "";
            mpScreen = MpScreen.Join;
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(bx, by + 2f * (bh + 14f), bw, bh), "Back", btn))
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
        GUIStyle btn = PaperButton(22);
        if (GUI.Button(new Rect(bx, by + 18f, bw, bh), "Join", btn))
        {
            Click();
            PersistName();
            mpError = "";
            NetworkSession.Ensure().JoinGame(mpJoinInput, mpName);
            mpScreen = MpScreen.Connecting;
        }
        GUI.enabled = true;

        if (GUI.Button(new Rect(bx, by + 18f + bh + 14f, bw, bh), "Back", btn))
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

        if (GUI.Button(new Rect(cx - 110f, Screen.height * 0.52f, 220f, 48f), "Cancel", PaperButton(22)))
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
            GUI.Button(new Rect(cx - 70f, Screen.height * 0.355f, 140f, 28f), "Copy", PaperButton(15)))
        {
            Click();
            CopyToClipboard(ns.Address);
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
                "• " + nm + "  -  " + TDBoardBuilder.ThemeName(TDBoardBuilder.ClampTheme(players[i].Theme)),
                Style(16, TextAnchor.MiddleLeft, new Color(0.9f, 0.92f, 0.95f)));
        }

        // ---- your board (every player picks their own) -------------------
        // Sits to the left of the roster column so it doesn't fight the list
        // length for vertical space.
        float bbw = 44f, bbh = 38f;
        float bbx = cx - 500f;
        GUI.Label(new Rect(bbx, listY, 244f, 26f), "Your board:", Style(17, TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.85f)));
        if (GUI.Button(new Rect(bbx, listY + 30f, bbw, bbh), "<", PaperButton(18)))
        {
            Click();
            SelectLocalTheme(-1);
        }
        GUI.Label(new Rect(bbx + bbw + 8f, listY + 30f, 160f, bbh),
            TDBoardBuilder.ThemeName(ActiveTheme), Style(17, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.7f)));
        if (GUI.Button(new Rect(bbx + bbw + 176f, listY + 30f, bbw, bbh), ">", PaperButton(18)))
        {
            Click();
            SelectLocalTheme(1);
        }
        GUIStyle boardBlurb = Style(12, TextAnchor.UpperLeft, new Color(0.72f, 0.76f, 0.82f));
        boardBlurb.wordWrap = true;
        GUI.Label(new Rect(bbx, listY + 76f, 268f, 62f), TDBoardBuilder.ThemeBlurb(ActiveTheme), boardBlurb);
        GUI.Label(new Rect(bbx, listY + 140f, 268f, 44f),
            "Only your own board changes - everyone else keeps theirs.",
            Style(12, TextAnchor.UpperLeft, new Color(0.6f, 0.63f, 0.68f)));

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
                GUIStyle st = PaperButton(14);
                st.normal.textColor = Color.black;
                st.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
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
            if (GUI.Button(new Rect(cx - 150f, ay, 300f, 52f), "Start Game", PaperButton(22)))
            {
                Click();
                ns.StartMatch();
            }
            GUI.enabled = true;
            GUI.Label(new Rect(cx - 260f, ay + 56f, 520f, 22f),
                "Up to " + NetConfig.MaxPlayers + " players. You can start at any time.",
                Style(13, TextAnchor.MiddleCenter, new Color(0.65f, 0.68f, 0.72f)));
        }

        if (GUI.Button(new Rect(cx - 100f, ay + 88f, 200f, 40f), "Leave", PaperButton(20)))
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

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void TD_CopyToClipboard(string text);
#endif

    /// <summary>Copies text to the clipboard. WebGL needs a JS bridge
    /// (GUIUtility.systemCopyBuffer does nothing in the browser).</summary>
    static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        TD_CopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }

    void Click()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Click();
    }
}
