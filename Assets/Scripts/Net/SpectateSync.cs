using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Streams board snapshots for spectating.
///
/// Every peer captures its own board and ships it to the host. The host relays a
/// board's snapshot <b>only to clients watching that board</b>, so bandwidth
/// scales with how many boards are actually being watched rather than N x N.
/// </summary>
public class SpectateSync : MonoBehaviour
{
    public static SpectateSync Instance { get; private set; }

    static class Msg
    {
        public const string Snap = "td.snap";    // owner -> host
        public const string Relay = "td.relay";  // host -> watchers
        public const string Watch = "td.watch";  // client -> host
    }

    const float SnapshotInterval = 0.12f;   // ~8 Hz

    private bool host;
    private float timer;
    private ulong watching;                  // board id this client is watching (0 = none)

    private readonly Dictionary<ulong, HashSet<ulong>> watchers = new Dictionary<ulong, HashSet<ulong>>();
    private readonly BoardSnapshot local = new BoardSnapshot();
    private readonly BoardSnapshot incoming = new BoardSnapshot();
    private readonly List<ulong> idScratch = new List<ulong>();

    public ulong Watching { get { return watching; } }

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
        watchers.Clear();
        watching = 0;
        timer = 0f;

        NetworkSession.RegisterNamed(Msg.Snap, OnSnap);
        NetworkSession.RegisterNamed(Msg.Relay, OnRelay);
        NetworkSession.RegisterNamed(Msg.Watch, OnWatch);
    }

    public void SetWatching(ulong boardId)
    {
        if (watching == boardId) return;
        watching = boardId;

        using var w = new FastBufferWriter(8, Allocator.Temp);
        w.WriteValueSafe(boardId);
        NetworkSession.SendNamedToServer(Msg.Watch, w);
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
            // the host's own board goes straight to anyone watching it
            RelayToWatchers(NetworkSession.LocalClientId, local);
        }
        else
        {
            using var w = new FastBufferWriter(2048, Allocator.Temp);
            local.Write(w);
            NetworkSession.SendNamedToServer(Msg.Snap, w);
        }
    }

    void OnSnap(ulong sender, FastBufferReader reader)
    {
        if (!host) return;
        incoming.Read(reader);

        RelayToWatchers(sender, incoming);

        if (watching == sender && TDGameManager.Instance != null)
            TDGameManager.Instance.ApplyRemoteSnapshot(sender, incoming);
    }

    void RelayToWatchers(ulong boardId, BoardSnapshot snap)
    {
        if (!host) return;

        HashSet<ulong> set;
        if (!watchers.TryGetValue(boardId, out set) || set.Count == 0) return;

        using var w = new FastBufferWriter(2056, Allocator.Temp);
        w.WriteValueSafe(boardId);
        snap.Write(w);

        idScratch.Clear();
        foreach (ulong id in set) idScratch.Add(id);
        NetworkSession.SendNamedToClients(Msg.Relay, idScratch, w);
    }

    void OnWatch(ulong sender, FastBufferReader reader)
    {
        if (!host) return;
        reader.ReadValueSafe(out ulong boardId);

        foreach (var kv in watchers) kv.Value.Remove(sender);

        if (boardId != 0)
        {
            HashSet<ulong> set;
            if (!watchers.TryGetValue(boardId, out set))
            {
                set = new HashSet<ulong>();
                watchers[boardId] = set;
            }
            set.Add(sender);
        }
    }

    void OnRelay(ulong sender, FastBufferReader reader)
    {
        if (host) return;
        reader.ReadValueSafe(out ulong boardId);
        if (boardId != watching) return;

        incoming.Read(reader);
        if (TDGameManager.Instance != null)
            TDGameManager.Instance.ApplyRemoteSnapshot(boardId, incoming);
    }
}
