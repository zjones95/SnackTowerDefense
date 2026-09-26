using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-authoritative match state for multiplayer: the shared wave clock and
/// every board's lives/money/wave/cleared/eliminated.
///
/// The host owns the wave; clients simulate their own board and report state.
/// A wave advances once every non-eliminated board has cleared it.
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
        public bool Cleared;
        public bool Eliminated;
        public bool HasStatus;   // false until we've heard from this board
    }

    static class Msg
    {
        public const string State = "td.state";   // client -> host
        public const string Boards = "td.boards"; // host -> all
    }

    public readonly List<BoardState> Boards = new List<BoardState>();
    public int Wave { get; private set; }
    public bool Prep { get; private set; }
    public bool Over { get; private set; }
    public bool Victory { get; private set; }

    private bool host;
    private float prepTimer;
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

        Over = false; Victory = false; Prep = true;
        Wave = 1; prepTimer = TDBalance.PrepDuration;
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
                    Cleared = false,
                    Eliminated = false,
                    HasStatus = false
                });
            }
        }

        NetworkSession.RegisterNamed(Msg.State, OnStateMessage);
        NetworkSession.RegisterNamed(Msg.Boards, OnBoardsMessage);

        if (host)
        {
            MarkLocalBoard();
            Broadcast();
        }

        if (TDGameManager.Instance != null)
            TDGameManager.Instance.MatchWaveStart(Wave, TDBalance.PrepDuration);
    }

    // ---------------------------------------------------------------- update
    void Update()
    {
        if (NetworkSession.Instance == null || !NetworkSession.Instance.InLobby) return;

        if (host)
        {
            if (Over) return;

            pruneTimer -= Time.deltaTime;
            if (pruneTimer <= 0f) { pruneTimer = 1f; PruneDisconnected(); }

            if (Prep)
            {
                prepTimer -= Time.deltaTime;
                if (prepTimer <= 0f)
                {
                    Prep = false;
                    Broadcast();
                    if (TDGameManager.Instance != null) TDGameManager.Instance.BeginWaveFromMatch();
                }
            }
            else
            {
                if (AllEliminated()) EndMatch(false);
                else if (AllAliveCleared()) Advance();
            }
        }
        else
        {
            // clients report their board periodically as well as on change
            sendTimer -= Time.deltaTime;
            if (sendTimer <= 0f) { sendTimer = 0.5f; SendLocalState(); }
        }
    }

    void Advance()
    {
        if (Wave >= TDBalance.TotalWaves) { EndMatch(true); return; }

        Wave++;
        Prep = false;   // after the opening wave, start the moment every board is clear
        for (int i = 0; i < Boards.Count; i++)
        {
            Boards[i].Cleared = false;
            Boards[i].Wave = Wave;
        }
        Broadcast();
        if (TDGameManager.Instance != null)
            TDGameManager.Instance.BeginWaveFromMatch();
    }

    void EndMatch(bool victory)
    {
        Over = true;
        Victory = victory;
        Broadcast();
        if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(victory);
    }

    bool AllEliminated()
    {
        if (Boards.Count == 0) return false;
        for (int i = 0; i < Boards.Count; i++)
            if (!Boards[i].Eliminated) return false;
        return true;
    }

    bool AllAliveCleared()
    {
        bool anyAlive = false;
        for (int i = 0; i < Boards.Count; i++)
        {
            BoardState b = Boards[i];
            if (b.Eliminated) continue;
            anyAlive = true;
            if (!b.Cleared) return false;
        }
        return anyAlive;
    }

    void PruneDisconnected()
    {
        bool changed = false;
        for (int i = Boards.Count - 1; i >= 0; i--)
        {
            if (!NetworkSession.IsConnected(Boards[i].ClientId))
            {
                Boards.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Broadcast();
    }

    // -------------------------------------------------------------- reporting
    /// <summary>Called by the local board whenever its state changes.</summary>
    public void ReportLocal(int lives, int money, int wave, bool cleared, bool eliminated)
    {
        if (host)
        {
            BoardState b = LocalBoard();
            if (b != null)
            {
                b.Lives = lives; b.Money = money; b.Wave = wave;
                b.Cleared = cleared; b.Eliminated = eliminated; b.HasStatus = true;
            }
            Broadcast();
        }
        else
        {
            SendState(lives, money, wave, cleared, eliminated);
        }
    }

    void MarkLocalBoard()
    {
        BoardState b = LocalBoard();
        if (b == null) return;
        var gm = TDGameManager.Instance;
        b.Lives = TDBalance.StartLives;
        b.Money = TDBalance.StartMoney;
        b.Wave = 1;
        b.Cleared = false;
        b.Eliminated = false;
        b.HasStatus = true;
        if (gm != null) { b.Lives = gm.Lives; b.Money = gm.Money; }
    }

    BoardState LocalBoard()
    {
        ulong id = NetworkSession.LocalClientId;
        for (int i = 0; i < Boards.Count; i++)
            if (Boards[i].ClientId == id) return Boards[i];
        return null;
    }

    void SendLocalState()
    {
        var gm = TDGameManager.Instance;
        if (gm == null) return;
        SendState(gm.Lives, gm.Money, gm.Wave, gm.Cleared, gm.Eliminated);
    }

    static void SendState(int lives, int money, int wave, bool cleared, bool eliminated)
    {
        using var w = new FastBufferWriter(32, Allocator.Temp);
        w.WriteValueSafe(lives);
        w.WriteValueSafe(money);
        w.WriteValueSafe(wave);
        w.WriteValueSafe(cleared);
        w.WriteValueSafe(eliminated);
        NetworkSession.SendNamedToServer(Msg.State, w);
    }

    void OnStateMessage(ulong sender, FastBufferReader reader)
    {
        if (!host) return;
        reader.ReadValueSafe(out int lives);
        reader.ReadValueSafe(out int money);
        reader.ReadValueSafe(out int wave);
        reader.ReadValueSafe(out bool cleared);
        reader.ReadValueSafe(out bool eliminated);

        BoardState b = BoardFor(sender);
        if (b == null)
        {
            b = new BoardState { ClientId = sender, Name = "Player " + sender };
            Boards.Add(b);
        }
        b.Lives = lives; b.Money = money; b.Wave = wave;
        b.Cleared = cleared; b.Eliminated = eliminated; b.HasStatus = true;
        Broadcast();
    }

    void OnBoardsMessage(ulong sender, FastBufferReader reader)
    {
        if (host) return;

        reader.ReadValueSafe(out int wave);
        reader.ReadValueSafe(out bool prep);
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
            reader.ReadValueSafe(out bool cleared);
            reader.ReadValueSafe(out bool eliminated);
            Boards.Add(new BoardState
            {
                ClientId = id, Name = bname, Lives = lives, Money = money, Wave = bwave,
                Cleared = cleared, Eliminated = eliminated, HasStatus = true
            });
        }

        int prevWave = Wave;
        bool prevPrep = Prep;
        Wave = wave; Prep = prep;

        if (over && !Over)
        {
            Over = true; Victory = victory;
            if (TDGameManager.Instance != null) TDGameManager.Instance.OnMatchOver(victory);
            return;
        }

        if (wave != prevWave)
        {
            if (TDGameManager.Instance != null)
            {
                // Opening wave: honour the prep timer. Later waves: begin at once.
                TDGameManager.Instance.MatchWaveStart(wave, prep ? TDBalance.PrepDuration : 0f);
                if (!prep) TDGameManager.Instance.BeginWaveFromMatch();
            }
        }
        else if (prevPrep && !prep)
        {
            if (TDGameManager.Instance != null) TDGameManager.Instance.BeginWaveFromMatch();
        }
    }

    void Broadcast()
    {
        if (!host) return;
        using var w = new FastBufferWriter(16 + NetConfig.MaxPlayers * 72, Allocator.Temp);
        w.WriteValueSafe(Wave);
        w.WriteValueSafe(Prep);
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
            w.WriteValueSafe(b.Cleared);
            w.WriteValueSafe(b.Eliminated);
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
