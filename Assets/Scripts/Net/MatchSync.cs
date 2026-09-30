using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Per-board STATE RELAY for multiplayer (issue #7).
///
/// INDEPENDENT WAVES: every peer owns its own wave number and runs waves exactly
/// like single player -- clearing your wave starts your next one immediately.
/// There is no shared wave clock, so one slow board can no longer stall the rest.
///
/// MatchSync does NOT decide when a wave starts. It only relays each board's
/// state (name / wave / lives / money / gold generated / tower value / cleared /
/// eliminated) so the scoreboard stays accurate:
///     client -> host : "td.state"  (on change, plus a periodic keep-alive)
///     host   -> all  : "td.boards" (the full roster)
///
/// PER-PLAYER RESULTS: clearing all <see cref="TDBalance.TotalWaves"/> waves
/// finishes only THAT player's own board (their end screen shows Victory;
/// they stop playing while the match continues for everyone else).
/// Elimination still only ends that player's own board -- they keep
/// spectating. The host ends the match as a defeat (Over, Victory=false)
/// only once every board is eliminated or gone; each client applies only its
/// OWN row for its end screen and ignores other boards' clears.
/// </summary>
public class MatchSync : MonoBehaviour
{
    public static MatchSync Instance { get; private set; }

    public class BoardState
    {
        public ulong ClientId;
        public string Name = "";
        public int Lives;
        public int Money;
        public int Wave;
        public int GoldGenerated;   // total money the local Gold towers made
        public int TowerValue;      // total invested cost of the local board's towers
        public bool Cleared;
        public bool Eliminated;
        public bool HasStatus;   // false until we've heard from this board
        /// <summary>Install guid (memory only, never on the wire): lets a
        /// rejoin adopt its tombstoned row under a fresh ClientId.</summary>
        public string Guid = "";
        /// <summary>Link dropped but seat held. Treated like elimination for the
        /// defeat check; cleared by the next live report. Expires after the
        /// reconnect grace period, freeing the seat.</summary>
        public bool Absent;
        public float AbsentSince;
    }

    static class Msg
    {
        public const string State = "td.state";   // client -> host
        public const string Boards = "td.boards"; // host -> all
    }

    public readonly List<BoardState> Boards = new List<BoardState>();
    public bool Over { get; private set; }
    public bool Victory { get; private set; }

    /// <summary>This peer's own wave number (independent of the other boards).</summary>
    public int LocalWave { get; private set; }

    private bool host;
    private bool localPrep;          // opening prep only; later waves start at once
    private float localPrepTimer;
    private bool localEliminated;
    private bool localCleared;       // this board cleared every wave (own victory; match continues)
    private float sendTimer;
    private float pruneTimer;

    public static MatchSync Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("MatchSync");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<MatchSync>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginMatch()
    {
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;

        Over = false; Victory = false;
        LocalWave = 1;
        localPrep = true;
        localPrepTimer = TDBalance.PrepDuration;
        localEliminated = false;
        localCleared = false;
        sendTimer = 0f; pruneTimer = 0f;

        Boards.Clear();
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null)
        {
            for (int i = 0; i < ns.Players.Count; i++)
            {
                Boards.Add(new BoardState
                {
                    ClientId = ns.Players[i].ClientId,
                    Name = ns.Players[i].Name,
                    Lives = TDBalance.StartLives,
                    Money = TDBalance.StartMoney,
                    Wave = 1,
                    GoldGenerated = 0,
                    TowerValue = 0,
                    Cleared = false,
                    Eliminated = false,
                    HasStatus = false,
                    Guid = ns.Players[i].Guid ?? ""
                });
            }
        }

        NetworkSession.RegisterNamed(Msg.State, OnStateMessage);
        NetworkSession.RegisterNamed(Msg.Boards, OnBoardsMessage);

        MarkLocalBoard();
        if (host) Broadcast();

        // Opening wave: each peer runs its own prep, then starts wave 1 itself.
        if (TDGameManager.Instance != null)
            TDGameManager.Instance.MatchWaveStart(LocalWave, TDBalance.PrepDuration);
    }

    // ---------------------------------------------------------------- update
    void Update()
    {
        // InSession (not just InLobby) so the local row stays live while the
        // link is down: the board keeps simulating and reports on rejoin.
        if (NetworkSession.Instance == null || !NetworkSession.Instance.InSession) return;

        if (host)
        {
            pruneTimer -= Time.deltaTime;
            if (pruneTimer <= 0f) { pruneTimer = 1f; PruneDisconnected(); }
        }

        if (Over) return;

        // Every peer runs its own opening prep, then begins its first wave.
        if (localPrep)
        {
            localPrepTimer -= Time.deltaTime;
            if (localPrepTimer <= 0f)
            {
                localPrep = false;
                BeginLocalWave(LocalWave);
            }
        }

        // Keep the scoreboard fresh even while nothing changes.
        sendTimer -= Time.deltaTime;
        if (sendTimer <= 0f) { sendTimer = 0.5f; SendLocalState(); }
    }

    // -------------------------------------------------------- local wave flow
    void BeginLocalWave(int wave)
    {
        if (Over || localEliminated || localCleared) return;
        LocalWave = wave;
        if (TDGameManager.Instance != null) TDGameManager.Instance.BeginWaveFromMatch();
        PublishLocal();
    }

    /// <summary>Advances this board past the wave it just cleared. No other
    /// board is consulted -- waves are independent.</summary>
    void AdvanceLocal(int clearedWave)
    {
        if (clearedWave >= TDBalance.TotalWaves)
        {
            // Per-player victory: only this board is done. The match
            // continues for everyone else (global Over stays false).
            LocalWave = clearedWave;
            localCleared = true;
            localPrep = false;
            EndLocal(true);
            return;
        }

        LocalWave = clearedWave + 1;
        localPrep = false;
        if (TDGameManager.Instance != null)
        {
            // Same as single player: the next wave starts the moment the last
            // mob dies, with no prep.
            TDGameManager.Instance.MatchWaveStart(LocalWave, 0f);
            TDGameManager.Instance.BeginWaveFromMatch();
        }
    }

    void EndMatch(bool victory)
    {
        if (Over) return;
        Over = true;
        Victory = victory;
        PublishLocal();   // relay our final row, then the result
        if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(victory);
    }

    /// <summary>Per-player finish: shows this peer's own end screen without
    /// ending the match for anyone else. The final row is still relayed so
    /// the scoreboard marks this board cleared/out.</summary>
    void EndLocal(bool victory)
    {
        PublishLocal();   // relay our final row (Over stays false)
        if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(victory);
    }

    // ------------------------------------------------------------ reporting
    /// <summary>Called by the local board whenever its state changes (clear,
    /// life lost, elimination). A clear advances this board immediately.
    /// GoldGenerated / TowerValue default to the live local board; a caller can
    /// pass explicit values but normally need not.</summary>
    public void ReportLocal(int lives, int money, int wave, bool cleared, bool eliminated,
                            int goldGenerated = -1, int towerValue = -1)
    {
        if (cleared && !eliminated && !localEliminated && !localCleared)
        {
            AdvanceLocal(wave);
        }
        else
        {
            LocalWave = wave;
            if (eliminated) { localEliminated = true; localPrep = false; }
        }

        if (!Over) PublishLocal(goldGenerated, towerValue);
    }

    /// <summary>Refreshes this peer's scoreboard row and relays it.</summary>
    void PublishLocal(int goldGenerated = -1, int towerValue = -1)
    {
        TDGameManager gm = TDGameManager.Instance;
        int lives = gm != null ? gm.Lives : 0;
        int money = gm != null ? gm.Money : 0;
        int wave = gm != null ? gm.Wave : LocalWave;
        bool cleared = gm != null && gm.Cleared;
        bool eliminated = gm != null && gm.Eliminated;
        int gold = goldGenerated >= 0 ? goldGenerated : (gm != null ? gm.GoldGenerated : 0);
        int tower = towerValue >= 0 ? towerValue : (gm != null ? gm.LocalTowerValue() : 0);
        LocalWave = wave;

        ApplyLocalRow(lives, money, wave, cleared, eliminated, gold, tower);

        if (host)
        {
            CheckMatchEnd();
            Broadcast();
        }
        else
        {
            SendState(lives, money, wave, cleared, eliminated, gold, tower);
        }
    }

    void SendLocalState()
    {
        // Identical to a change report; the keep-alive just refreshes the row.
        PublishLocal();
    }

    void ApplyLocalRow(int lives, int money, int wave, bool cleared, bool eliminated,
                       int goldGenerated, int towerValue)
    {
        BoardState b = LocalBoard();
        if (b == null)
        {
            b = new BoardState
            {
                ClientId = NetworkSession.LocalClientId,
                Name = "Player",
                Guid = NetworkSession.Instance != null ? NetworkSession.Instance.LocalGuid : ""
            };
            Boards.Add(b);
        }
        b.Lives = lives; b.Money = money; b.Wave = wave;
        b.GoldGenerated = goldGenerated; b.TowerValue = towerValue;
        b.Cleared = cleared; b.Eliminated = eliminated; b.HasStatus = true;
    }

    /// <summary>Mid-match rejoin resync: reports the live local board (which
    /// kept simulating through the outage) so the host adopts our tombstoned
    /// row. Never resets LocalWave or the board.</summary>
    public void RejoinSync()
    {
        NetworkSession.RegisterNamed(Msg.State, OnStateMessage);
        NetworkSession.RegisterNamed(Msg.Boards, OnBoardsMessage);
        MarkLocalBoard();
        PublishLocal();
    }

    void MarkLocalBoard()
    {
        TDGameManager gm = TDGameManager.Instance;
        int wave = gm != null ? gm.Wave : LocalWave;
        LocalWave = wave;
        ApplyLocalRow(gm != null ? gm.Lives : TDBalance.StartLives,
                      gm != null ? gm.Money : TDBalance.StartMoney,
                      wave,
                      gm != null && gm.Cleared,
                      gm != null && gm.Eliminated,
                      gm != null ? gm.GoldGenerated : 0,
                      gm != null ? gm.LocalTowerValue() : 0);
    }

    BoardState LocalBoard()
    {
        ulong id = NetworkSession.LocalClientId;
        for (int i = 0; i < Boards.Count; i++)
            if (Boards[i].ClientId == id) return Boards[i];
        return null;
    }

    static void SendState(int lives, int money, int wave, bool cleared, bool eliminated,
                          int goldGenerated, int towerValue)
    {
        using var w = new FastBufferWriter(48, Allocator.Temp);
        w.WriteValueSafe(lives);
        w.WriteValueSafe(money);
        w.WriteValueSafe(wave);
        w.WriteValueSafe(goldGenerated);
        w.WriteValueSafe(towerValue);
        w.WriteValueSafe(cleared);
        w.WriteValueSafe(eliminated);
        NetworkSession.SendNamedToServer(Msg.State, w);
    }

    void OnStateMessage(ulong sender, FastBufferReader reader)
    {
        if (!host || Over) return;
        reader.ReadValueSafe(out int lives);
        reader.ReadValueSafe(out int money);
        reader.ReadValueSafe(out int wave);
        reader.ReadValueSafe(out int goldGenerated);
        reader.ReadValueSafe(out int towerValue);
        reader.ReadValueSafe(out bool cleared);
        reader.ReadValueSafe(out bool eliminated);

        BoardState b = BoardFor(sender);
        if (b == null)
        {
            // A rejoin carries a fresh ClientId: adopt its tombstoned row by
            // install guid so the seat (and scoreboard history) survives.
            string guid = NetworkSession.Instance != null ? NetworkSession.Instance.GuidForClient(sender) : "";
            if (!string.IsNullOrEmpty(guid))
            {
                for (int i = 0; i < Boards.Count; i++)
                    if (Boards[i].Guid == guid) { b = Boards[i]; break; }
            }
            if (b == null)
            {
                b = new BoardState { ClientId = sender, Name = "Player " + sender, Guid = guid };
                Boards.Add(b);
            }
            else
            {
                b.ClientId = sender;
            }
        }
        b.Lives = lives; b.Money = money; b.Wave = wave;
        b.GoldGenerated = goldGenerated; b.TowerValue = towerValue;
        b.Cleared = cleared; b.Eliminated = eliminated; b.HasStatus = true;
        b.Absent = false;

        CheckMatchEnd();
        Broadcast();
    }

    void OnBoardsMessage(ulong sender, FastBufferReader reader)
    {
        if (host) return;

        reader.ReadValueSafe(out bool over);
        reader.ReadValueSafe(out bool victory);
        reader.ReadValueSafe(out int count);

        Boards.Clear();
        for (int i = 0; i < count; i++)
        {
            reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out string bname);
            reader.ReadValueSafe(out int lives);
            reader.ReadValueSafe(out int money);
            reader.ReadValueSafe(out int bwave);
            reader.ReadValueSafe(out int goldGenerated);
            reader.ReadValueSafe(out int towerValue);
            reader.ReadValueSafe(out bool cleared);
            reader.ReadValueSafe(out bool eliminated);
            reader.ReadValueSafe(out bool absent);
            Boards.Add(new BoardState
            {
                ClientId = id, Name = bname, Lives = lives, Money = money, Wave = bwave,
                GoldGenerated = goldGenerated, TowerValue = towerValue,
                Cleared = cleared, Eliminated = eliminated, HasStatus = true, Absent = absent
            });
        }

        // The host's copy of our own row is up to one report interval old, so
        // keep this peer's row live from the local board.
        MarkLocalBoard();

        // Per-player results: only our OWN row drives our end screen. Another
        // board's clear must never finish us; the global Over now means defeat
        // only (every board eliminated) and applies to survivors still playing.
        // (Own-row victory is normally applied immediately in AdvanceLocal;
        // this echo covers a missed/stale local report.)
        if (!Over)
        {
            BoardState own = LocalBoard();
            if (own != null && !own.Eliminated && own.Cleared && own.Wave >= TDBalance.TotalWaves)
            {
                localCleared = true;
                localPrep = false;
                if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(true);
            }
            else if (over)
            {
                Over = true; Victory = false;
                if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(false);
            }
        }
    }

    // -------------------------------------------------------------- match end
    void CheckMatchEnd()
    {
        if (Over) return;

        // Per-player results: a clear finishes only that board (handled
        // locally in AdvanceLocal / OnBoardsMessage, never here). The host
        // ends the match as a defeat only once every board is out.
        // Nobody can win once every board is out (absent boards count as out:
        // their sim died with their link from the host's point of view).
        if (AllEliminated()) EndMatch(false);
    }

    bool AllEliminated()
    {
        if (Boards.Count == 0) return false;
        for (int i = 0; i < Boards.Count; i++)
            if (!Boards[i].Eliminated && !Boards[i].Absent) return false;
        return true;
    }

    /// <summary>A drop holds its seat instead of freeing it: the row is marked
    /// absent (survivors see "reconnecting") and expires after the reconnect
    /// grace period, freeing the seat only then.</summary>
    void PruneDisconnected()
    {
        bool changed = false;
        for (int i = Boards.Count - 1; i >= 0; i--)
        {
            BoardState b = Boards[i];
            if (NetworkSession.IsConnected(b.ClientId))
            {
                if (b.Absent) { b.Absent = false; changed = true; }
                continue;
            }
            if (!b.Absent)
            {
                b.Absent = true;
                b.AbsentSince = Time.time;
                changed = true;
            }
            else if (Time.time - b.AbsentSince >= NetworkSession.ReconnectGraceSeconds)
            {
                Boards.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) { CheckMatchEnd(); Broadcast(); }
    }

    // ------------------------------------------------------------------ wire
    void Broadcast()
    {
        if (!host) return;
        using var w = new FastBufferWriter(16 + NetConfig.MaxPlayers * 80, Allocator.Temp);
        w.WriteValueSafe(Over);
        w.WriteValueSafe(Victory);
        w.WriteValueSafe(Boards.Count);
        for (int i = 0; i < Boards.Count; i++)
        {
            BoardState b = Boards[i];
            w.WriteValueSafe(b.ClientId);
            w.WriteValueSafe(b.Name ?? "");
            w.WriteValueSafe(b.Lives);
            w.WriteValueSafe(b.Money);
            w.WriteValueSafe(b.Wave);
            w.WriteValueSafe(b.GoldGenerated);
            w.WriteValueSafe(b.TowerValue);
            w.WriteValueSafe(b.Cleared);
            w.WriteValueSafe(b.Eliminated);
            w.WriteValueSafe(b.Absent);
        }
        NetworkSession.SendNamedToAll(Msg.Boards, w);
    }

    public BoardState BoardFor(ulong clientId)
    {
        for (int i = 0; i < Boards.Count; i++)
            if (Boards[i].ClientId == clientId) return Boards[i];
        return null;
    }
}
