using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Streams board snapshots so every board is live for everyone.
///
/// Every peer captures its own board and ships it to the host, which fans each
/// board's snapshot out to all other clients. Snapshots go over
/// <see cref="NetworkDelivery.UnreliableSequenced"/>: they are self-contained
/// full states, so dropping a stale one is better than retransmitting it.
/// </summary>
public class SpectateSync : MonoBehaviour
{
    public static SpectateSync Instance { get; private set; }

    static class Msg
    {
        public const string Snap = "td.snap";    // owner -> host
        public const string Relay = "td.relay";  // host -> everyone else
    }

    // Issue #15: raised from 0.12 s (~8 Hz) to 0.05 s (~20 Hz) for smoother
    // remote motion and to catch short-lived projectiles.
    //
    // Rough bandwidth at a large wave (30 mobs, 25 towers, 15 projectiles):
    //   mobs   30 x 10 B = 300 B
    //   towers 25 x  7 B = 175 B
    //   projs  15 x 10 B = 150 B
    //   ~0.6 KB packed + headers, so ~5 KB/s at 8 Hz -> ~12.7 KB/s at 20 Hz
    //   per board (plus the same again when the host relays it). Transient FX
    //   events add a few tens of bytes only when something actually fires.
    const float SnapshotInterval = 0.05f;

    private bool host;
    private float timer;

    private readonly BoardSnapshot local = new BoardSnapshot();
    private readonly BoardSnapshot incoming = new BoardSnapshot();
    private readonly List<ulong> targets = new List<ulong>();

    // One writer, reset and refilled each snapshot, instead of allocating a
    // native buffer every tick. Persistent so it survives frames; freed in
    // OnDestroy. SendNamedMessage serialises the buffer before returning, so
    // reusing it after the call is safe.
    private FastBufferWriter writer;
    private bool writerReady;

    public static SpectateSync Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("SpectateSync");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SpectateSync>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (!writerReady) return;
        writer.Dispose();
        writerReady = false;
    }

    public void Begin()
    {
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;
        timer = 0f;
        EnsureWriter();

        NetworkSession.RegisterNamed(Msg.Snap, OnSnap);
        NetworkSession.RegisterNamed(Msg.Relay, OnRelay);
    }

    void EnsureWriter()
    {
        if (writerReady) return;
        writer = new FastBufferWriter(2056, Allocator.Persistent);
        writerReady = true;
    }

    void Update()
    {
        if (NetworkSession.Instance == null || !NetworkSession.Instance.InLobby) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = SnapshotInterval;

        local.Capture();

        if (host)
        {
            // the host's own board goes straight to every client
            RelayToPeers(NetworkSession.LocalClientId, local);
        }
        else
        {
            EnsureWriter();
            writer.Truncate(0);
            local.Write(writer);
            NetworkSession.SendNamedToServer(Msg.Snap, writer, NetworkDelivery.UnreliableSequenced);
        }
    }

    void OnSnap(ulong sender, FastBufferReader reader)
    {
        if (!host) return;
        incoming.Read(reader);

        RelayToPeers(sender, incoming);

        // the host plays its own board natively, but mirrors the others
        if (TDGameManager.Instance != null)
            TDGameManager.Instance.ApplyRemoteSnapshot(sender, incoming);
    }

    void RelayToPeers(ulong boardId, BoardSnapshot snap)
    {
        if (!host) return;

        NetworkSession.FillRelayTargets(targets, boardId);
        if (targets.Count == 0) return;

        EnsureWriter();
        writer.Truncate(0);
        writer.WriteValueSafe(boardId);
        snap.Write(writer);

        NetworkSession.SendNamedToClients(Msg.Relay, targets, writer, NetworkDelivery.UnreliableSequenced);
    }

    void OnRelay(ulong sender, FastBufferReader reader)
    {
        if (host) return;

        reader.ReadValueSafe(out ulong boardId);
        incoming.Read(reader);

        if (TDGameManager.Instance != null)
            TDGameManager.Instance.ApplyRemoteSnapshot(boardId, incoming);
    }
}
