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

    const float SnapshotInterval = 0.12f;   // ~8 Hz

    private bool host;
    private float timer;

    private readonly BoardSnapshot local = new BoardSnapshot();
    private readonly BoardSnapshot incoming = new BoardSnapshot();
    private readonly List<ulong> targets = new List<ulong>();

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

    public void Begin()
    {
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;
        timer = 0f;

        NetworkSession.RegisterNamed(Msg.Snap, OnSnap);
        NetworkSession.RegisterNamed(Msg.Relay, OnRelay);
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
            using var w = new FastBufferWriter(2048, Allocator.Temp);
            local.Write(w);
            NetworkSession.SendNamedToServer(Msg.Snap, w, NetworkDelivery.UnreliableSequenced);
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

        using var w = new FastBufferWriter(2056, Allocator.Temp);
        w.WriteValueSafe(boardId);
        snap.Write(w);

        NetworkSession.SendNamedToClients(Msg.Relay, targets, w, NetworkDelivery.UnreliableSequenced);
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
