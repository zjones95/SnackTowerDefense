using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Transient cosmetic FX (splash bursts, tracer bolts) on their OWN best-effort
/// channel, deliberately separate from <see cref="BoardSnapshot"/>:
///
///   td.fx     owner -> host
///   td.fxall  host  -> the other clients (boardId prefix, like td.relay)
///
/// Keeping cosmetics off the simulation snapshot means future visual work can
/// change this format without touching the board wire format / <c>GameVersion</c>,
/// and the events can be dropped or throttled freely without desyncing gameplay.
/// Events are best-effort (unreliable-sequenced): a lost batch is a missed
/// sparkle, nothing more. Positions are board-local (the receiver adds its own
/// board offset), mirroring the snapshot convention.
/// </summary>
public class FxSync : MonoBehaviour
{
    public static FxSync Instance { get; private set; }

    static class Msg
    {
        public const string Fx = "td.fx";       // owner -> host
        public const string FxAll = "td.fxall"; // host -> other clients
    }

    const int MaxBatch = 48;         // events per message (matches FxEvents.MaxPending)
    const float SendInterval = 0.05f; // same cadence as the snapshot

    private bool host;
    private float timer;

    private readonly List<FxEvent> outgoing = new List<FxEvent>();
    private readonly List<FxEvent> incoming = new List<FxEvent>();
    private readonly List<ulong> targets = new List<ulong>();

    private FastBufferWriter writer;
    private bool writerReady;

    public static FxSync Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("FxSync");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<FxSync>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (writerReady) { writer.Dispose(); writerReady = false; }
    }

    public void Begin()
    {
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;
        timer = 0f;
        EnsureWriter();
        NetworkSession.RegisterNamed(Msg.Fx, OnFxMessage);
        NetworkSession.RegisterNamed(Msg.FxAll, OnFxAllMessage);
    }

    void EnsureWriter()
    {
        if (writerReady) return;
        writer = new FastBufferWriter(1024, Allocator.Persistent);
        writerReady = true;
    }

    void Update()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || !ns.InLobby) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = SendInterval;

        FxEvents.Drain(outgoing);
        if (outgoing.Count == 0) return;

        TDGameManager gm = TDGameManager.Instance;
        Vector3 off = gm != null ? gm.BoardOffset : Vector3.zero;

        EnsureWriter();
        writer.Truncate(0);
        if (host)
        {
            // the host's own board goes out to every client
            writer.WriteValueSafe(NetworkSession.LocalClientId);
            WriteBatch(writer, outgoing, off);
            NetworkSession.FillRelayTargets(targets, NetworkSession.LocalClientId);
            NetworkSession.SendNamedToClients(Msg.FxAll, targets, writer, NetworkDelivery.UnreliableSequenced);
        }
        else
        {
            WriteBatch(writer, outgoing, off);
            NetworkSession.SendNamedToServer(Msg.Fx, writer, NetworkDelivery.UnreliableSequenced);
        }
    }

    void OnFxMessage(ulong sender, FastBufferReader reader)
    {
        if (!host) return;

        ReadBatch(reader, incoming);
        if (incoming.Count == 0) return;

        // fan out to the other clients (events are already board-local)
        EnsureWriter();
        writer.Truncate(0);
        writer.WriteValueSafe(sender);
        WriteBatch(writer, incoming, Vector3.zero);
        NetworkSession.FillRelayTargets(targets, sender);
        NetworkSession.SendNamedToClients(Msg.FxAll, targets, writer, NetworkDelivery.UnreliableSequenced);

        ApplyToBoard(sender, incoming);
    }

    void OnFxAllMessage(ulong sender, FastBufferReader reader)
    {
        if (host) return;

        reader.ReadValueSafe(out ulong boardId);
        if (boardId == NetworkSession.LocalClientId) return;   // our own board already played it

        ReadBatch(reader, incoming);
        ApplyToBoard(boardId, incoming);
    }

    static void ApplyToBoard(ulong boardId, List<FxEvent> events)
    {
        TDGameManager gm = TDGameManager.Instance;
        if (gm != null) gm.ApplyRemoteFx(boardId, events);
    }

    // ------------------------------------------------------------ wire helpers
    static byte Col(float v) { return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255); }

    static void WriteBatch(FastBufferWriter w, List<FxEvent> list, Vector3 off)
    {
        int n = Mathf.Min(list.Count, MaxBatch);
        w.WriteValueSafe((byte)n);
        for (int i = 0; i < n; i++)
        {
            FxEvent e = list[i];
            Vector3 a = e.From - off;
            Vector3 b = e.To - off;
            w.WriteValueSafe((byte)e.Kind);
            w.WriteValueSafe(BoardSnapshot.Enc(a.x));
            w.WriteValueSafe(BoardSnapshot.Enc(a.y));
            w.WriteValueSafe(BoardSnapshot.Enc(a.z));
            w.WriteValueSafe(BoardSnapshot.Enc(b.x));
            w.WriteValueSafe(BoardSnapshot.Enc(b.y));
            w.WriteValueSafe(BoardSnapshot.Enc(b.z));
            w.WriteValueSafe((short)Mathf.Clamp(Mathf.RoundToInt(e.Radius * 100f), 0, 32000));
            w.WriteValueSafe(Col(e.A.r)); w.WriteValueSafe(Col(e.A.g)); w.WriteValueSafe(Col(e.A.b));
            w.WriteValueSafe(Col(e.B.r)); w.WriteValueSafe(Col(e.B.g)); w.WriteValueSafe(Col(e.B.b));
        }
    }

    static void ReadBatch(FastBufferReader r, List<FxEvent> into)
    {
        into.Clear();
        r.ReadValueSafe(out byte n);
        for (int i = 0; i < n; i++)
        {
            r.ReadValueSafe(out byte kind);
            r.ReadValueSafe(out short x); r.ReadValueSafe(out short y); r.ReadValueSafe(out short z);
            r.ReadValueSafe(out short ex); r.ReadValueSafe(out short ey); r.ReadValueSafe(out short ez);
            r.ReadValueSafe(out short rad);
            r.ReadValueSafe(out byte rr); r.ReadValueSafe(out byte gg); r.ReadValueSafe(out byte bb);
            r.ReadValueSafe(out byte r2); r.ReadValueSafe(out byte g2); r.ReadValueSafe(out byte b2);

            into.Add(new FxEvent
            {
                Kind = (FxKind)kind,
                From = new Vector3(BoardSnapshot.Dec(x), BoardSnapshot.Dec(y), BoardSnapshot.Dec(z)),
                To = new Vector3(BoardSnapshot.Dec(ex), BoardSnapshot.Dec(ey), BoardSnapshot.Dec(ez)),
                Radius = rad / 100f,
                A = new Color(rr / 255f, gg / 255f, bb / 255f),
                B = new Color(r2 / 255f, g2 / 255f, b2 / 255f)
            });
        }
    }
}
