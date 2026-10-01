using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>Board-local SFX over a small best-effort channel. Owner hears its own
/// board immediately; the host relays tagged events, and receivers play only
/// events from their currently viewed board. UI clicks and music stay local.</summary>
public enum BoardSound : byte { Shot, Death, Leak, Build, Merge, RoundClear }

public class BoardAudioSync : MonoBehaviour
{
    public static BoardAudioSync Instance { get; private set; }

    const string Audio = "td.audio";
    const string AudioAll = "td.audioall";
    const int MaxBatch = 48;
    const float SendInterval = .05f;

    struct SoundEvent
    {
        public BoardSound Sound;
        public byte Tower;
    }

    readonly List<SoundEvent> pending = new List<SoundEvent>();
    readonly List<SoundEvent> incoming = new List<SoundEvent>();
    readonly List<ulong> targets = new List<ulong>();
    FastBufferWriter writer;
    bool writerReady;
    bool host;
    float timer;

    public static BoardAudioSync Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("BoardAudioSync");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<BoardAudioSync>();
        return Instance;
    }

    public static void Queue(BoardSound sound, byte tower = 0)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (Instance == null || ns == null || !ns.InSession || !ns.LinkUp) return;
        if (Instance.pending.Count < MaxBatch)
            Instance.pending.Add(new SoundEvent { Sound = sound, Tower = tower });
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (writerReady) writer.Dispose();
        if (Instance == this) Instance = null;
    }

    public void Begin()
    {
        pending.Clear();
        incoming.Clear();
        timer = 0f;
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;
        if (!writerReady)
        {
            writer = new FastBufferWriter(128, Allocator.Persistent);
            writerReady = true;
        }
        NetworkSession.RegisterNamed(Audio, OnAudio);
        NetworkSession.RegisterNamed(AudioAll, OnAudioAll);
    }

    void Update()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || !ns.InSession || !ns.LinkUp)
        {
            pending.Clear();
            return;
        }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = SendInterval;
        if (pending.Count == 0) return;

        writer.Truncate(0);
        if (host)
        {
            writer.WriteValueSafe(NetworkSession.LocalClientId);
            WriteBatch(writer, pending);
            NetworkSession.FillRelayTargets(targets, NetworkSession.LocalClientId);
            NetworkSession.SendNamedToClients(AudioAll, targets, writer, NetworkDelivery.UnreliableSequenced);
        }
        else
        {
            WriteBatch(writer, pending);
            NetworkSession.SendNamedToServer(Audio, writer, NetworkDelivery.UnreliableSequenced);
        }
        pending.Clear();
    }

    void OnAudio(ulong sender, FastBufferReader reader)
    {
        if (!host || !NetworkSession.IsConnected(sender)) return;
        ReadBatch(reader, incoming);
        if (incoming.Count == 0) return;

        writer.Truncate(0);
        writer.WriteValueSafe(sender);
        WriteBatch(writer, incoming);
        NetworkSession.FillRelayTargets(targets, sender);
        NetworkSession.SendNamedToClients(AudioAll, targets, writer, NetworkDelivery.UnreliableSequenced);
        PlayIfViewed(sender, incoming); // host spectators receive directly
    }

    void OnAudioAll(ulong sender, FastBufferReader reader)
    {
        if (host || sender != NetworkManager.ServerClientId) return;
        reader.ReadValueSafe(out ulong boardId);
        if (boardId == NetworkSession.LocalClientId) return;
        ReadBatch(reader, incoming);
        PlayIfViewed(boardId, incoming);
    }

    static void PlayIfViewed(ulong boardId, List<SoundEvent> events)
    {
        TDGameManager gm = TDGameManager.Instance;
        TDAudio audio = TDAudio.Instance;
        if (gm == null || audio == null || !gm.HearingRemoteBoard(boardId)) return;
        for (int i = 0; i < events.Count; i++)
            audio.PlayRemoteBoard(events[i].Sound, events[i].Tower);
    }

    static void WriteBatch(FastBufferWriter w, List<SoundEvent> events)
    {
        w.WriteValueSafe((byte)events.Count);
        for (int i = 0; i < events.Count; i++)
        {
            w.WriteValueSafe((byte)events[i].Sound);
            w.WriteValueSafe(events[i].Tower);
        }
    }

    static void ReadBatch(FastBufferReader r, List<SoundEvent> events)
    {
        events.Clear();
        r.ReadValueSafe(out byte count);
        if (count > MaxBatch) return;
        for (int i = 0; i < count; i++)
        {
            r.ReadValueSafe(out byte sound);
            r.ReadValueSafe(out byte tower);
            if (sound <= (byte)BoardSound.RoundClear)
                events.Add(new SoundEvent { Sound = (BoardSound)sound, Tower = tower });
        }
    }
}
