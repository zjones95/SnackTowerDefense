using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A remote player's board: room + play-mat (static) plus, when the local player
/// is spectating it, a live mirror of their mobs, towers and projectiles driven
/// by streamed snapshots.
/// </summary>
public class RemoteBoard : MonoBehaviour
{
    public ulong ClientId;
    public string PlayerName = "Player";
    public Vector3 BoardOffset;

    private TDMap map;
    private Transform nameplate;
    private TextMesh plateText;
    private Transform liveRoot;

    private class RemoteMob
    {
        public GameObject Go;
        public Transform HpRoot;
        public Transform Fill;
        public MobStatusIcons Icons;
        public MobDef Def;
        public float HpFraction;
        public Vector3 Target;
        public byte Status;
        public byte Stacks;
    }

    private class RemoteTower
    {
        public GameObject Go;
        public Transform Turret;
        public byte Type;
        public byte Tier;
    }

    private readonly Dictionary<ushort, RemoteMob> mobs = new Dictionary<ushort, RemoteMob>();
    private readonly Dictionary<int, RemoteTower> towers = new Dictionary<int, RemoteTower>();
    private readonly Dictionary<int, GameObject> projs = new Dictionary<int, GameObject>();

    private readonly List<ushort> mobSeen = new List<ushort>();
    private readonly List<int> towerSeen = new List<int>();
    private readonly List<int> projSeen = new List<int>();
    private readonly List<ushort> mobGone = new List<ushort>();
    private readonly List<int> towerGone = new List<int>();
    private readonly List<int> projGone = new List<int>();

    private static Material projMat;

    public static RemoteBoard Create(Transform parent, Vector3 offset, ulong clientId, string playerName)
    {
        GameObject go = new GameObject("RemoteBoard_" + playerName);
        go.transform.SetParent(parent, false);
        RemoteBoard rb = go.AddComponent<RemoteBoard>();
        rb.ClientId = clientId;
        rb.PlayerName = playerName;
        rb.BoardOffset = offset;

        rb.map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, offset);
        TDBoardBuilder.BuildTiles(go.transform, rb.map);
        TDBoardBuilder.BuildRoom(go.transform, rb.map, offset);

        GameObject live = new GameObject("Live");
        live.transform.SetParent(go.transform, false);
        rb.liveRoot = live.transform;

        rb.BuildNameplate(offset);
        return rb;
    }

    void BuildNameplate(Vector3 offset)
    {
        GameObject plate = new GameObject("BoardName");
        plate.transform.SetParent(transform, false);
        plate.transform.position = offset + new Vector3(0f, 4.5f, 0f);
        nameplate = plate.transform;

        TextMesh tm = plate.AddComponent<TextMesh>();
        tm.text = PlayerName;
        tm.characterSize = 0.30f;
        tm.fontSize = 90;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.96f, 0.75f);
        plateText = tm;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = plate.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }
    }

    public void SetStatus(string status)
    {
        if (plateText == null) return;
        string s = PlayerName + "\n" + status;
        if (plateText.text != s) plateText.text = s;
    }

    // ------------------------------------------------------------- rendering
    public void Apply(BoardSnapshot s)
    {
        SyncMobs(s);
        SyncTowers(s);
        SyncProjectiles(s);
    }

    /// <summary>The boss currently on this board (if any), for the on-screen boss bar.</summary>
    public bool TryGetBoss(out string name, out float fraction)
    {
        bool slowed, poisoned;
        int stacks;
        return TryGetBoss(out name, out fraction, out slowed, out poisoned, out stacks);
    }

    /// <summary>As above, but also reports the boss's slow/poison state for the
    /// boss HUD's status icons.</summary>
    public bool TryGetBoss(out string name, out float fraction,
                           out bool slowed, out bool poisoned, out int stacks)
    {
        foreach (var kv in mobs)
        {
            RemoteMob rm = kv.Value;
            if (rm.Def != null && rm.Def.archetype == MobArchetype.Boss)
            {
                name = rm.Def.displayName;
                fraction = rm.HpFraction;
                slowed = (rm.Status & 1) != 0;
                poisoned = rm.Stacks > 0;
                stacks = rm.Stacks;
                return true;
            }
        }
        name = null;
        fraction = 0f;
        slowed = false;
        poisoned = false;
        stacks = 0;
        return false;
    }

    void SyncMobs(BoardSnapshot s)
    {
        mobSeen.Clear();
        for (int i = 0; i < s.Mobs.Count; i++)
        {
            BoardSnapshot.MobSnap ms = s.Mobs[i];
            mobSeen.Add(ms.Id);

            RemoteMob rm;
            if (!mobs.TryGetValue(ms.Id, out rm))
            {
                GameObject go = new GameObject("RMob_" + ms.Id);
                go.transform.SetParent(liveRoot, false);

                MobDef def = MobCatalog.All[Mathf.Clamp(ms.Type, 0, MobCatalog.All.Length - 1)];
                Transform fill;
                Transform hpRoot = MobVisual.Build(go.transform, def, out fill, true);   // always bar remote mobs (incl. bosses)
                Vector3 p = BoardOffset + new Vector3(BoardSnapshot.Dec(ms.X), 0f, BoardSnapshot.Dec(ms.Z));
                go.transform.position = p;

                rm = new RemoteMob { Go = go, HpRoot = hpRoot, Fill = fill, Def = def, Target = p,
                                     Icons = MobStatusIcons.Get(hpRoot) };
                mobs[ms.Id] = rm;
            }

            rm.Target = BoardOffset + new Vector3(BoardSnapshot.Dec(ms.X), 0f, BoardSnapshot.Dec(ms.Z));
            rm.HpFraction = ms.Hp / 255f;
            MobVisual.SetFill(rm.Fill, rm.HpFraction);

            rm.Status = ms.Status;
            rm.Stacks = ms.Stacks;
            if (rm.Icons != null)
            {
                bool slowed = (ms.Status & 1) != 0;    // bit0 covers slow and stun
                rm.Icons.Set(slowed, ms.Stacks > 0, ms.Stacks);
            }
        }

        mobGone.Clear();
        foreach (var kv in mobs) if (!mobSeen.Contains(kv.Key)) mobGone.Add(kv.Key);
        for (int i = 0; i < mobGone.Count; i++)
        {
            if (mobs[mobGone[i]] != null && mobs[mobGone[i]].Go != null) Destroy(mobs[mobGone[i]].Go);
            mobs.Remove(mobGone[i]);
        }
    }

    void SyncTowers(BoardSnapshot s)
    {
        towerSeen.Clear();
        for (int i = 0; i < s.Towers.Count; i++)
        {
            BoardSnapshot.TowerSnap ts = s.Towers[i];
            int key = ts.Cy * 128 + ts.Cx;
            towerSeen.Add(key);

            RemoteTower rt;
            bool exists = towers.TryGetValue(key, out rt);
            // A merge (tier up) or re-roll (type change) replaces the tower at the
            // same cell, so rebuild when the type or tier differs from the snapshot.
            if (exists && (rt.Go == null || rt.Type != ts.Type || rt.Tier != ts.Tier))
            {
                if (rt.Go != null) Destroy(rt.Go);
                towers.Remove(key);
                exists = false;
                rt = null;
            }

            if (!exists)
            {
                GameObject go = new GameObject("RTower");
                go.transform.SetParent(liveRoot, false);
                go.transform.position = map.CellCenter(ts.Cx, ts.Cy);
                Transform turret = TowerVisual.Build(go.transform, (TowerType)ts.Type, ts.Tier);
                TowerVisual.BuildTierLabel(go.transform, ts.Tier);
                rt = new RemoteTower { Go = go, Turret = turret, Type = ts.Type, Tier = ts.Tier };
                towers[key] = rt;
            }

            if (rt.Turret != null)
                rt.Turret.rotation = Quaternion.Euler(0f, BoardSnapshot.YawDeg(ts.Yaw), 0f);
        }

        towerGone.Clear();
        foreach (var kv in towers) if (!towerSeen.Contains(kv.Key)) towerGone.Add(kv.Key);
        for (int i = 0; i < towerGone.Count; i++)
        {
            if (towers[towerGone[i]] != null && towers[towerGone[i]].Go != null) Destroy(towers[towerGone[i]].Go);
            towers.Remove(towerGone[i]);
        }
    }

    void SyncProjectiles(BoardSnapshot s)
    {
        if (projMat == null)
            projMat = TDVisuals.Mat(new Color(1f, 0.92f, 0.55f), 0.1f, 0.7f);

        projSeen.Clear();
        for (int i = 0; i < s.Projs.Count; i++)
        {
            BoardSnapshot.ProjSnap ps = s.Projs[i];
            projSeen.Add(ps.Id);

            GameObject go;
            if (!projs.TryGetValue(ps.Id, out go) || go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider c = go.GetComponent<Collider>();
                if (c != null) Destroy(c);
                go.name = "RProj";
                go.transform.SetParent(liveRoot, false);
                go.transform.localScale = Vector3.one * 0.22f;
                go.GetComponent<Renderer>().sharedMaterial = projMat;
                projs[ps.Id] = go;
            }
            go.transform.position = BoardOffset + new Vector3(BoardSnapshot.Dec(ps.X), BoardSnapshot.Dec(ps.Y), BoardSnapshot.Dec(ps.Z));
        }

        projGone.Clear();
        foreach (var kv in projs) if (!projSeen.Contains(kv.Key)) projGone.Add(kv.Key);
        for (int i = 0; i < projGone.Count; i++)
        {
            if (projs[projGone[i]] != null) Destroy(projs[projGone[i]]);
            projs.Remove(projGone[i]);
        }
    }

    void Update()
    {
        if (nameplate != null && Camera.main != null)
            nameplate.rotation = Camera.main.transform.rotation;

        // smooth remote mobs toward their latest snapshot position
        float k = 10f * Time.deltaTime;
        foreach (var kv in mobs)
        {
            RemoteMob rm = kv.Value;
            if (rm.Go == null) continue;
            rm.Go.transform.position = Vector3.Lerp(rm.Go.transform.position, rm.Target, Mathf.Clamp01(k));

            if (rm.HpRoot != null && Camera.main != null)
            {
                Vector3 dir = Camera.main.transform.position - rm.HpRoot.position;
                if (dir.sqrMagnitude > 0.001f) rm.HpRoot.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
