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
    /// <summary>Index into <see cref="TDBoardBuilder.PickerThemes"/>: the board
    /// its owner picked. Rebuilds the static dressing if it ever changes.</summary>
    public int Theme;

    private TDMap map;
    private Transform staticRoot;
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
        public float TargetYaw;   // degrees; the body slerps toward this each frame
        public byte Status;
        public byte Stacks;
    }

    private class RemoteTower
    {
        public GameObject Go;
        public Transform Turret;
        public byte Type;
        public byte Tier;
        public float TargetYaw;   // degrees; the turret slerps toward this each frame
    }

    private class RemoteProj
    {
        public GameObject Go;
        public Vector3 Target;
        public byte Type;
    }

    private readonly Dictionary<ushort, RemoteMob> mobs = new Dictionary<ushort, RemoteMob>();
    private readonly Dictionary<int, RemoteTower> towers = new Dictionary<int, RemoteTower>();
    private readonly Dictionary<int, RemoteProj> projs = new Dictionary<int, RemoteProj>();

    private readonly List<ushort> mobSeen = new List<ushort>();
    private readonly List<int> towerSeen = new List<int>();
    private readonly List<int> projSeen = new List<int>();
    private readonly List<ushort> mobGone = new List<ushort>();
    private readonly List<int> towerGone = new List<int>();
    private readonly List<int> projGone = new List<int>();

    // Per-type projectile visuals (cached), mirroring the local SpawnProjectile look:
    // the authored model when one exists, else a sphere tinted with the tower colour.
    private static readonly Dictionary<byte, Material> projMats = new Dictionary<byte, Material>();
    private static readonly Dictionary<byte, GameObject> projModels = new Dictionary<byte, GameObject>();

    static Material ProjMat(byte type)
    {
        Material m;
        if (projMats.TryGetValue(type, out m) && m != null) return m;
        m = TDVisuals.Mat(TowerCatalog.Get((TowerType)type).color, 0.1f, 0.7f);
        projMats[type] = m;
        return m;
    }

    static GameObject ProjModel(TowerType t)
    {
        GameObject g;
        if (projModels.TryGetValue((byte)t, out g)) return g;
        g = SnackModels.Load(SnackModels.ProjectilePath(t));   // null when none exists
        projModels[(byte)t] = g;
        return g;
    }

    public static RemoteBoard Create(Transform parent, Vector3 offset, ulong clientId, string playerName, int theme)
    {
        GameObject go = new GameObject("RemoteBoard_" + playerName);
        go.transform.SetParent(parent, false);
        RemoteBoard rb = go.AddComponent<RemoteBoard>();
        rb.ClientId = clientId;
        rb.PlayerName = playerName;
        rb.BoardOffset = offset;
        rb.BuildStatic(TDBoardBuilder.ClampTheme(theme));

        GameObject live = new GameObject("Live");
        live.transform.SetParent(go.transform, false);
        rb.liveRoot = live.transform;

        rb.BuildNameplate(offset);
        return rb;
    }

    /// <summary>Builds (or rebuilds) the board's static dressing — tiles and room
    /// — for the owner's chosen theme. Kept on its own child so a theme change can
    /// swap it without touching the live mobs/towers/projectiles.</summary>
    void BuildStatic(BoardTheme theme)
    {
        Theme = TDBoardBuilder.ThemeIndex(theme);
        if (staticRoot != null) Destroy(staticRoot.gameObject);

        GameObject go = new GameObject("Static");
        go.transform.SetParent(transform, false);
        staticRoot = go.transform;

        map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, BoardOffset);
        TDBoardBuilder.BuildTiles(go.transform, map, theme);
        TDBoardBuilder.BuildRoom(go.transform, map, BoardOffset, theme);
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
        string s = PlayerName + "\n" + TDBoardBuilder.ThemeName(TDBoardBuilder.ClampTheme(Theme)) + "\n" + status;
        if (plateText.text != s) plateText.text = s;
    }

    /// <summary>Repoints this board at a new ClientId (a rejoin carries a fresh
    /// one). Position and rendered content stay; snapshots resume under the id.
    /// A changed board choice rebuilds the static dressing.</summary>
    public void Reassign(ulong clientId, string playerName, int theme)
    {
        ClientId = clientId;
        PlayerName = playerName ?? PlayerName;
        gameObject.name = "RemoteBoard_" + PlayerName;
        if (theme != Theme) BuildStatic(TDBoardBuilder.ClampTheme(theme));
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
        bool slowed, poisoned, tarred;
        int stacks;
        return TryGetBoss(out name, out fraction, out slowed, out poisoned, out stacks, out tarred);
    }

    /// <summary>As above, but also reports the boss's slow/stun/poison/tar state
    /// for the boss HUD's status icons (dipped has no snapshot channel, so it
    /// stays local-only).</summary>
    public bool TryGetBoss(out string name, out float fraction,
                           out bool slowed, out bool poisoned, out int stacks, out bool tarred)
    {
        bool stunned;
        return TryGetBoss(out name, out fraction, out slowed, out stunned, out poisoned, out stacks, out tarred);
    }

    public bool TryGetBoss(out string name, out float fraction,
                           out bool slowed, out bool stunned, out bool poisoned, out int stacks, out bool tarred)
    {
        float sour;
        return TryGetBoss(out name, out fraction, out slowed, out stunned, out poisoned, out stacks, out tarred, out sour);
    }

    public bool TryGetBoss(out string name, out float fraction,
                           out bool slowed, out bool stunned, out bool poisoned, out int stacks, out bool tarred, out float sour)
    {
        foreach (var kv in mobs)
        {
            RemoteMob rm = kv.Value;
            if (rm.Def != null && rm.Def.archetype == MobArchetype.Boss)
            {
                name = rm.Def.displayName;
                fraction = rm.HpFraction;
                stunned = (rm.Status & 2) != 0;
                slowed = ((rm.Status & 1) != 0) && !stunned;
                poisoned = rm.Stacks > 0;
                stacks = rm.Stacks;
                tarred = (rm.Status & 4) != 0;
                sour = (rm.Status >> 4) * .05f;
                return true;
            }
        }
        name = null;
        fraction = 0f;
        slowed = false;
        stunned = false;
        poisoned = false;
        stacks = 0;
        tarred = false;
        sour = 0f;
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
                                     TargetYaw = BoardSnapshot.YawDeg(ms.Yaw),
                                     Icons = MobStatusIcons.Get(hpRoot) };
                mobs[ms.Id] = rm;
                go.transform.rotation = Quaternion.Euler(0f, rm.TargetYaw, 0f);
            }

            rm.Target = BoardOffset + new Vector3(BoardSnapshot.Dec(ms.X), 0f, BoardSnapshot.Dec(ms.Z));
            rm.TargetYaw = BoardSnapshot.YawDeg(ms.Yaw);   // smoothed in Update
            rm.HpFraction = ms.Hp / 255f;
            MobVisual.SetFill(rm.Fill, rm.HpFraction);

            rm.Status = ms.Status;
            rm.Stacks = ms.Stacks;
            if (rm.Icons != null)
            {
                bool stunned = (ms.Status & 2) != 0;   // bit1: stunned (distinct badge)
                bool slowed = (ms.Status & 1) != 0 && !stunned;
                bool tarred = (ms.Status & 4) != 0;    // bit2: tar (+damage taken)
                rm.Icons.Set(slowed, stunned, ms.Stacks > 0, ms.Stacks, tarred, false, 0f,
                    (ms.Status >> 4) * .05f);
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
                rt = new RemoteTower { Go = go, Turret = turret, Type = ts.Type, Tier = ts.Tier,
                                       TargetYaw = BoardSnapshot.YawDeg(ts.Yaw) };
                if (turret != null) turret.rotation = Quaternion.Euler(0f, rt.TargetYaw, 0f);
                towers[key] = rt;
            }

            rt.TargetYaw = BoardSnapshot.YawDeg(ts.Yaw);   // smoothed in Update
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
        projSeen.Clear();
        for (int i = 0; i < s.Projs.Count; i++)
        {
            BoardSnapshot.ProjSnap ps = s.Projs[i];
            projSeen.Add(ps.Id);
            Vector3 target = BoardOffset + new Vector3(BoardSnapshot.Dec(ps.X), BoardSnapshot.Dec(ps.Y), BoardSnapshot.Dec(ps.Z));

            RemoteProj rp;
            if (!projs.TryGetValue(ps.Id, out rp) || rp == null || rp.Go == null)
            {
                TowerType t = (TowerType)ps.Type;
                GameObject go;
                GameObject model = ProjModel(t);
                if (model != null)
                {
                    go = Instantiate(model);
                    go.name = "RProj_" + t;
                    go.transform.localScale = Vector3.one;
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Collider c = go.GetComponent<Collider>();
                    if (c != null) Destroy(c);
                    go.name = "RProj_" + t;
                    go.transform.localScale = Vector3.one * 0.22f;
                    go.GetComponent<Renderer>().sharedMaterial = ProjMat(ps.Type);
                }
                go.transform.SetParent(liveRoot, false);
                go.transform.position = target;
                rp = new RemoteProj { Go = go, Target = target, Type = ps.Type };
                projs[ps.Id] = rp;
            }
            rp.Target = target;
        }

        projGone.Clear();
        foreach (var kv in projs) if (!projSeen.Contains(kv.Key)) projGone.Add(kv.Key);
        for (int i = 0; i < projGone.Count; i++)
        {
            RemoteProj rp = projs[projGone[i]];
            if (rp != null && rp.Go != null) Destroy(rp.Go);
            projs.Remove(projGone[i]);
        }
    }

    /// <summary>Replays the owner's transient cosmetic events (splash bursts,
    /// tracer bolts), received board-local, at this board's world offset.</summary>
    public void ReplayFx(List<FxEvent> events)
    {
        for (int i = 0; i < events.Count; i++)
        {
            FxEvent e = events[i];
            e.From += BoardOffset;
            e.To += BoardOffset;
            FxEvents.Play(e);
        }
    }

    void Update()
    {
        if (nameplate != null && Camera.main != null)
            nameplate.rotation = Camera.main.transform.rotation;

        // Frame-rate independent smoothing factor shared by mobs, projectiles and
        // turret aim, so remote boards render smoothly at the display frame rate
        // (60 fps) instead of stepping at the ~15 Hz snapshot rate.
        float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
        foreach (var kv in mobs)
        {
            RemoteMob rm = kv.Value;
            if (rm.Go == null) continue;
            rm.Go.transform.position = Vector3.Lerp(rm.Go.transform.position, rm.Target, Mathf.Clamp01(k));
            rm.Go.transform.rotation = Quaternion.Slerp(rm.Go.transform.rotation,
                Quaternion.Euler(0f, rm.TargetYaw, 0f), Mathf.Clamp01(k));

            if (rm.HpRoot != null && Camera.main != null)
            {
                Vector3 dir = Camera.main.transform.position - rm.HpRoot.position;
                if (dir.sqrMagnitude > 0.001f) rm.HpRoot.rotation = Quaternion.LookRotation(dir);
            }
        }

        // projectiles lerp toward their latest snapshot position too
        foreach (var kv in projs)
        {
            RemoteProj rp = kv.Value;
            if (rp == null || rp.Go == null) continue;

            Vector3 cur = rp.Go.transform.position;
            Vector3 next = Vector3.Lerp(cur, rp.Target, Mathf.Clamp01(k));

            // the skewer rod must point along its travel (the local FirePierce
            // orients it with FromToRotation(up, dir))
            if ((TowerType)rp.Type == TowerType.Pierce)
            {
                Vector3 dir = next - cur;
                if (dir.sqrMagnitude > 0.000001f)
                    rp.Go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
            rp.Go.transform.position = next;
        }

        // turret aim slerps toward the latest snapshot yaw (was snapping at ~15 Hz)
        foreach (var kv in towers)
        {
            RemoteTower rt = kv.Value;
            if (rt == null || rt.Turret == null) continue;
            rt.Turret.rotation = Quaternion.Slerp(rt.Turret.rotation,
                Quaternion.Euler(0f, rt.TargetYaw, 0f), Mathf.Clamp01(k));
        }
    }
}
