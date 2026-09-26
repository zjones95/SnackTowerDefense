using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public TowerType Type;
    public int Tier = 1;
    public int CellX, CellY;

    private float cooldown;
    private Transform turret;
    private bool isSelected;

    // where shots leave the model: half its height, just clear of its body
    private float muzzleY = 0.6f;
    private float muzzleForward = 0.5f;

    public TowerTierStats Stats { get { return TowerCatalog.Get(Type).Stats(Tier); } }
    public string DisplayName { get { return TowerCatalog.Get(Type).displayName; } }
    public float TurretYaw { get { return turret != null ? turret.eulerAngles.y : 0f; } }
    /// <summary>Selection state (the yellow tile outline is drawn by the manager).</summary>
    public bool IsSelected { get { return isSelected; } }

    public void Setup(TowerType type, int tier, int cx, int cy)
    {
        Type = type; Tier = tier; CellX = cx; CellY = cy;
        BuildVisual();
        cooldown = 0f;
    }

    void BuildVisual()
    {
        turret = TowerVisual.Build(transform, Type, Tier);

        // measure the model so shots leave from its middle, not the floor
        if (turret != null)
        {
            Renderer[] rs = turret.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                muzzleY = Mathf.Max(0.25f, b.size.y * 0.5f);
                muzzleForward = Mathf.Max(0.20f, Mathf.Max(b.extents.x, b.extents.z) + 0.10f);
            }
        }

        TowerVisual.BuildTierLabel(transform, Tier);
    }

    /// <summary>Selection is only state now — the visual is the yellow tile
    /// outline drawn by TDGameManager over the selected tower's cell.</summary>
    public void SetSelected(bool on)
    {
        isSelected = on;
    }

    void Update()
    {
        TowerTierStats s = Stats;
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;

        Mob target = null;
        float best = float.MinValue;
        if (mobs != null)
        {
            for (int i = 0; i < mobs.Count; i++)
            {
                Mob m = mobs[i];
                if (m == null) continue;
                if (Vector3.Distance(transform.position, m.transform.position) <= s.range && m.Progress > best)
                {
                    best = m.Progress;
                    target = m;
                }
            }
        }

        if (turret != null && target != null)
        {
            Vector3 dir = target.transform.position - turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        if (Type == TowerType.Slow)
        {
            FireGumballs(s);
            return;
        }

        if (target == null) return;
        cooldown = s.fireInterval;
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);

        Vector3 muzzle = Muzzle();

        switch (Type)
        {
            case TowerType.Sniper:
                target.TakeDamage(s.damage);
                Tracer(muzzle, target.transform.position + Vector3.up * 0.4f);
                break;
            case TowerType.Pierce:
                FirePierce(muzzle, target, s);
                break;
            case TowerType.Chain:
                FireChain(muzzle, target, s);
                break;
            default:
                SpawnProjectile(muzzle, target, s);
                break;
        }
    }

    void FirePierce(Vector3 from, Mob target, TowerTierStats s)
    {
        Vector3 dir = target.transform.position - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        GameObject go;

        GameObject prefab = SnackModels.Load(SnackModels.ProjectilePath(Type));
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "SkewerRod";
            go.transform.position = from;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);  // modelled along +Z in Blender
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.name = "SkewerRod";
            go.transform.position = from;
            go.transform.localScale = new Vector3(0.05f, 0.30f, 0.05f);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            go.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(0.72f, 0.75f, 0.80f), 0.9f, 0.3f);
        }

        Transform parent = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        if (parent != null) go.transform.SetParent(parent, true);

        PierceProjectile p = go.AddComponent<PierceProjectile>();
        p.Dir = dir;
        p.Speed = 9f;                      // slow: it visibly walks to the enemy
        p.Damage = s.damage;
        p.Width = s.pierceWidth;
        p.MaxHits = Mathf.Max(1, s.pierceCount);
        p.MaxDistance = s.range;
    }

    void FireChain(Vector3 from, Mob target, TowerTierStats s)
    {
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        List<Mob> hit = new List<Mob>();
        List<Vector3> pts = new List<Vector3>();

        Mob cur = target;
        for (int i = 0; i <= s.chainCount && cur != null; i++)
        {
            Vector3 cp = cur.transform.position + Vector3.up * 0.4f;
            pts.Add(cp);
            hit.Add(cur);
            cur.TakeDamage(s.damage * Mathf.Pow(0.75f, i));

            Mob next = null;
            float best = s.chainRange;
            if (mobs != null)
            {
                for (int j = 0; j < mobs.Count; j++)
                {
                    Mob m = mobs[j];
                    if (m == null || hit.Contains(m)) continue;
                    float d = Vector3.Distance(cp, m.transform.position);
                    if (d <= best) { best = d; next = m; }
                }
            }
            cur = next;
        }

        for (int i = 0; i < pts.Count; i++) Tracer(i == 0 ? from : pts[i - 1], pts[i]);
    }

    // ---------------------------------------------------------- gumball slow
    static readonly Color[] GumColours =
    {
        new Color(0.30f, 0.85f, 0.35f),   // green
        new Color(0.90f, 0.18f, 0.20f),   // red
        new Color(0.62f, 0.35f, 0.90f),   // purple
        new Color(0.30f, 0.85f, 0.35f),
        new Color(0.90f, 0.18f, 0.20f),
        new Color(0.62f, 0.35f, 0.90f)
    };

    static readonly Dictionary<Color, Material> gumMats = new Dictionary<Color, Material>();

    static Material GumMat(Color c)
    {
        Material m;
        if (gumMats.TryGetValue(c, out m) && m != null) return m;
        m = TDVisuals.Mat(c, 0.05f, 0.85f);
        gumMats[c] = m;
        return m;
    }

    void FireGumballs(TowerTierStats s)
    {
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        if (mobs == null || mobs.Count == 0) return;

        // gather everything in range, then favour whatever is furthest along
        List<Mob> inRange = new List<Mob>();
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null) continue;
            if (Vector3.Distance(transform.position, m.transform.position) <= s.range)
                inRange.Add(m);
        }
        if (inRange.Count == 0) return;
        inRange.Sort((a, b) => b.Progress.CompareTo(a.Progress));

        int shots = Mathf.Min(s.multiShot > 0 ? s.multiShot : 1, inRange.Count);
        cooldown = s.fireInterval;
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);

        Vector3 muzzle = Muzzle();
        for (int i = 0; i < shots; i++)
            SpawnGumball(muzzle, inRange[i], s, GumColours[i % GumColours.Length]);
    }

    /// <summary>Mid-height of the model, nudged forward along the aim direction.</summary>
    Vector3 Muzzle()
    {
        Vector3 up = Vector3.up * muzzleY;
        Vector3 fwd = turret != null ? turret.forward : transform.forward;
        return transform.position + up + fwd * muzzleForward;
    }

    void SpawnGumball(Vector3 from, Mob target, TowerTierStats s, Color colour)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        go.name = "Gumball";
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.17f;
        go.GetComponent<Renderer>().sharedMaterial = GumMat(colour);

        Transform parent = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        if (parent != null) go.transform.SetParent(parent, true);

        Projectile p = go.AddComponent<Projectile>();
        p.Target = target;
        p.Speed = 14f;
        p.Damage = 0f;
        p.SlowFactor = s.slowFactor;
        p.SlowDuration = s.slowDuration;
        p.Tint = colour;
    }

    void SpawnProjectile(Vector3 from, Mob target, TowerTierStats s)
    {
        GameObject go;

        GameObject prefab = SnackModels.Load(SnackModels.ProjectilePath(Type));
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "Projectile";
            go.transform.position = from;
            go.transform.localScale = Vector3.one;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.name = "Projectile";
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.22f;
            TowerDef def = TowerCatalog.Get(Type);
            go.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(def.color, 0.1f, 0.6f);
        }

        Transform parent = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        if (parent != null) go.transform.SetParent(parent, true);

        Projectile p = go.AddComponent<Projectile>();
        p.Target = target;
        p.Speed = s.projectileSpeed;
        p.Damage = s.damage;
        p.SplashRadius = s.splashRadius;
        p.Bounces = s.bounceCount;
        p.BounceRange = s.bounceRange;
        p.PoisonDps = s.poisonDps;
        p.PoisonDuration = s.poisonDuration;
        p.GoldPerHit = s.goldPerHit;
        p.Tint = TowerCatalog.Get(Type).color;
        p.Spin = Type != TowerType.Splash;   // the soda blob wobbles instead
    }

    // A fast "bolt" of sour energy: a wide, flat ribbon that always faces the
    // camera, with a hot core down the middle.
    void Tracer(Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        Vector3 mid = a + dir * 0.5f;
        Vector3 fwd = dir / len;

        Vector3 toCam = Camera.main != null ? Camera.main.transform.position - mid : Vector3.up;
        Vector3 right = Vector3.Cross(fwd, toCam);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(fwd, Vector3.up);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, fwd).normalized;

        Quaternion rot = Quaternion.LookRotation(fwd, up);

        BoltPart("BoltGlow", mid, rot, new Vector3(0.190f, 0.050f, len), new Color(0.95f, 1f, 0.10f));
        BoltPart("BoltCore", mid, rot, new Vector3(0.075f, 0.022f, len * 1.01f), new Color(1f, 1f, 0.80f));
    }

    void BoltPart(string name, Vector3 pos, Quaternion rot, Vector3 scale, Color col)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        g.name = name;
        g.transform.position = pos;
        g.transform.rotation = rot;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(col, 0f, 0.85f);
        Destroy(g, 0.09f);
    }
}
