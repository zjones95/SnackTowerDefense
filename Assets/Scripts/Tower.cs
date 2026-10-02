using System.Collections.Generic;
using UnityEngine;

/// <summary>How a tower chooses which mob to shoot.</summary>
public enum TowerTargeting { Default, Nearest, Farthest, Random, HighestHealth, LowestHealth }

public partial class Tower : MonoBehaviour
{
    public TowerType Type;
    public int Tier = 1;
    public int CellX, CellY;
    public TowerTargeting Targeting = TowerTargeting.Default;
    public float DamageDone;   // total damage this tower has dealt (selection panel)
    public int GoldEarned;   // total gold this tower has paid out (selection panel)
    public int InvestedCost;   // total money sunk into this tower (for selling)

    private float cooldown;
    private Transform turret;
    private bool isSelected;
    private Transform rangeRing;

    static Material ringMat;

    /// <summary>Opaque dark-grey range outline, shared by every tower so the ring
    /// reads the same regardless of type. The Fx shader is alpha-blended, so a
    /// white texture with alpha 1 on the stroke and 0 inside gives a crisp ring.</summary>
    static Material RingMat()
    {
        if (ringMat != null) return ringMat;
        Color grey = new Color(0.17f, 0.17f, 0.19f);
        Shader fx = Shader.Find("Snack/Fx");
        if (fx != null)
        {
            ringMat = new Material(fx);
            ringMat.mainTexture = TDTextures.RangeDisc();
            if (ringMat.HasProperty("_Color")) ringMat.SetColor("_Color", new Color(grey.r, grey.g, grey.b, 1f));
        }
        else
        {
            ringMat = TDVisuals.TransparentMat(grey, 0.75f, 0.4f);
        }
        return ringMat;
    }

    // Random targeting keeps one choice so the turret doesn't jitter; it is
    // re-rolled after each shot.
    private Mob randomTarget;

    // Sniper T6 "Deadeye": consecutive hits on one target build a damage/crit ramp.
    private Mob deadeyeTarget;
    private int deadeyeStacks;

    // Boba T7 "Bobarista": the fire rate ramps up while it keeps firing.
    // Switching targets does NOT reset the spin; it resets only after 5s
    // without firing (tracked via bobaLastFire).
    private float bobaSpin;
    private float bobaLastFire = -99f;

    // where shots leave the model: half its height, just clear of its body
    private float muzzleY = 0.6f;
    private float muzzleForward = 0.5f;

    public TowerTierStats Stats { get { return TowerCatalog.Get(Type).Stats(Tier); } }
    public string DisplayName { get { return TowerCatalog.Get(Type).displayName; } }
    public float TurretYaw { get { return turret != null ? turret.eulerAngles.y : 0f; } }

    /// <summary>Range after roguelike mods (never mutates the shared catalog stats).</summary>
    float EffRange(TowerTierStats s) { return RogueMods.EffRange(s.range); }

    bool BossWaveNow()
    {
        TDGameManager gm = TDGameManager.Instance;
        return gm != null && RogueMods.IsBossWave(gm.Wave);
    }
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

        tierLabel = TowerVisual.BuildTierLabel(transform, Tier);
        BuildRangeRing();
    }

    /// <summary>Faint range disc shown while selected (10% opacity, just above ground).</summary>
    void BuildRangeRing()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Collider rc = ring.GetComponent<Collider>();
        if (rc != null) Destroy(rc);
        ring.name = "RangeRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        ring.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ring.GetComponent<Renderer>().sharedMaterial = RingMat();
        Renderer rr = ring.GetComponent<Renderer>();
        if (rr != null)
        {
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rr.receiveShadows = false;
        }
        rangeRing = ring.transform;
        rangeRing.gameObject.SetActive(isSelected);
    }

    /// <summary>Selection is only state now — the visual is the yellow tile
    /// outline drawn by TDGameManager over the selected tower's cell.</summary>
    public void SetSelected(bool on)
    {
        isSelected = on;
        if (rangeRing != null) rangeRing.gameObject.SetActive(on);
    }

    /// <summary>Changes the targeting mode (random re-rolls on the next shot).</summary>
    public void SetTargeting(TowerTargeting t)
    {
        Targeting = t;
        randomTarget = null;
    }

    /// <summary>Records damage this tower dealt (shown on the selection panel).</summary>
    public void AddDamage(float amount)
    {
        if (amount > 0f) DamageDone += amount;
    }

    /// <summary>Best in-range target for the current mode, or null.</summary>
    Mob PickTarget(List<Mob> mobs, float range)
    {
        if (mobs == null) return null;
        if (Targeting == TowerTargeting.Random) return PickRandom(mobs, range);

        Mob best = null;
        float bestScore = float.MinValue;
        float bestProgress = float.MinValue;
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null) continue;
            float d = Vector3.Distance(transform.position, m.transform.position);
            if (d > range) continue;

            float score = Score(m, d);
            // ties fall back to whichever is furthest along (closest to finishing)
            if (best == null || score > bestScore || (score == bestScore && m.Progress > bestProgress))
            {
                best = m; bestScore = score; bestProgress = m.Progress;
            }
        }
        return best;
    }

    float Score(Mob m, float dist)
    {
        switch (Targeting)
        {
            case TowerTargeting.Nearest: return -dist;
            case TowerTargeting.Farthest: return dist;
            case TowerTargeting.HighestHealth: return m.Health;
            case TowerTargeting.LowestHealth: return -m.Health;
            default: return m.Progress;
        }
    }

    Mob PickRandom(List<Mob> mobs, float range)
    {
        if (randomTarget != null && mobs.Contains(randomTarget) &&
            Vector3.Distance(transform.position, randomTarget.transform.position) <= range)
            return randomTarget;

        int count = 0;
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m != null && Vector3.Distance(transform.position, m.transform.position) <= range) count++;
        }
        if (count == 0) { randomTarget = null; return null; }

        int pick = Random.Range(0, count);
        int seen = 0;
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null) continue;
            if (Vector3.Distance(transform.position, m.transform.position) > range) continue;
            if (seen == pick) { randomTarget = m; return m; }
            seen++;
        }
        return null;
    }

    void Update()
    {
        TowerTierStats s = Stats;
        UpdateSnackBuffs();
        if (rangeRing != null && rangeRing.gameObject.activeSelf)
        {
            float r = s.auraRadius > 0f ? s.auraRadius : EffRange(s);
            rangeRing.localScale = new Vector3(r * 2f, r * 2f, 1f);
        }
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;

        Mob target = PickTarget(mobs, EffRange(s));

        if (turret != null && target != null)
        {
            Vector3 dir = target.transform.position - turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir),
                    1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        // Deadeye (Sniper T6) resets the moment the target changes or is lost.
        if (Type == TowerType.Sniper && s.deadeyeRamp > 0f && target != deadeyeTarget)
        {
            deadeyeTarget = target;
            deadeyeStacks = 0;
        }

        // Bobarista (Boba T7) spins up while it has a target to shoot at, and
        // the spin survives target switches. It resets only after 5s without
        // firing (bobaLastFire is stamped on every shot below).
        if (Type == TowerType.BobaBlaster && s.rateMinInterval > 0f && s.spinUpTime > 0f)
        {
            if (target != null) bobaSpin += Time.deltaTime;
            else if (Time.time - bobaLastFire >= 5f) bobaSpin = 0f;
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        if (Type == TowerType.Slow)
        {
            FireGumballs(s);
            return;
        }

        if (target == null) return;
        if (Type == TowerType.PopTartToaster || Type == TowerType.CookieCrumbler || Type == TowerType.SourFizz)
        {
            FireNewSnackTower(target, s);
            if (Targeting == TowerTargeting.Random) randomTarget = null;
            return;
        }
        // Bobarista ramps the interval from fireInterval down to rateMinInterval.
        float interval = s.fireInterval;
        if (Type == TowerType.BobaBlaster && s.rateMinInterval > 0f && s.spinUpTime > 0f)
            interval = Mathf.Lerp(s.fireInterval, s.rateMinInterval, Mathf.Clamp01(bobaSpin / s.spinUpTime));
        cooldown = interval / (RogueMods.EffRate(BossWaveNow()) * SpeedMultiplier);
        if (Type == TowerType.BobaBlaster) bobaLastFire = Time.time;   // spin persists across targets; idleness resets it
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);

        Vector3 muzzle = Muzzle();

        switch (Type)
        {
            case TowerType.Sniper:
                FireSniper(muzzle, target, s);
                break;
            case TowerType.Pierce:
                FirePierce(muzzle, target, s);
                break;
            case TowerType.Chain:
                FireChain(muzzle, target, s);
                break;
            case TowerType.PizzaOven:
                FirePizza(target, s);
                break;
            case TowerType.FondueFountain:
                FireFondue(muzzle, target, s);
                break;
            default:
                // SingleShot T6 "Kettle Burst" fires a volley at several targets.
                if (s.multiShot > 1) SpawnVolley(muzzle, target, s);
                else SpawnProjectile(muzzle, target, s);
                break;
        }

        if (Targeting == TowerTargeting.Random) randomTarget = null;   // re-roll next shot
    }

    /// <summary>Orders in-range mobs best-first for the current mode (random = shuffled).</summary>
    void RankInRange(List<Mob> list)
    {
        if (Targeting == TowerTargeting.Random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Mob tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
            return;
        }

        list.Sort((a, b) =>
        {
            float da = Vector3.Distance(transform.position, a.transform.position);
            float db = Vector3.Distance(transform.position, b.transform.position);
            float sa = Score(a, da), sb = Score(b, db);
            if (sa != sb) return sb.CompareTo(sa);
            return b.Progress.CompareTo(a.Progress);   // tie: furthest along
        });
    }

    /// <summary>Sniper T5/T6: roll a crit (which can bypass armour) and apply
    /// the Deadeye ramp built up from consecutive hits on this target.</summary>
    void FireSniper(Vector3 from, Mob target, TowerTierStats s)
    {
        int stacks = s.deadeyeRamp > 0f ? deadeyeStacks : 0;

        float dmg = s.damage;
        if (s.deadeyeRamp > 0f)
            dmg *= Mathf.Min(1f + s.deadeyeRamp * stacks, s.deadeyeCap);

        float critChance = s.critChance + (s.deadeyeRamp > 0f ? s.deadeyeCrit * stacks : 0f);
        bool crit = critChance > 0f && Random.value < critChance;
        if (crit) dmg *= s.critMult;

        Vector3 hitPoint = target.transform.position + Vector3.up * 0.4f;
        if (crit && s.critPierceArmour) target.TakeDamageFromTowerIgnoringArmour(dmg, this);
        else target.TakeDamageFromTower(dmg, this);
        AddDamage(dmg);

        if (s.deadeyeRamp > 0f)
            deadeyeStacks = Mathf.Min(deadeyeStacks + 1, DeadeyeMaxStacks(s));

        Tracer(from, hitPoint, new Color(0.30f, 1f, 0.35f), new Color(0.85f, 1f, 0.85f));
    }

    static int DeadeyeMaxStacks(TowerTierStats s)
    {
        if (s.deadeyeRamp <= 0f) return 0;
        return Mathf.Max(0, Mathf.CeilToInt((s.deadeyeCap - 1f) / s.deadeyeRamp));
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
        p.Source = this;
        p.Type = Type;
        p.Dir = dir;
        p.Speed = 9f;                      // slow: it visibly walks to the enemy
        p.Damage = s.damage;
        p.Width = s.pierceWidth;
        p.MaxHits = Mathf.Max(1, s.pierceCount + (RogueMods.Artillery ? 1 : 0));
        p.MaxDistance = EffRange(s);
        p.Boomerang = s.boomerangReturn;   // Pierce T6: return pass at full damage
    }

    void FireChain(Vector3 from, Mob target, TowerTierStats s)
    {
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        List<Mob> hit = new List<Mob>();
        List<Vector3> pts = new List<Vector3>();
        List<int> parent = new List<int>();

        int maxTargets = Mathf.Max(1, s.chainCount + 1);   // total hit cap
        int fanout = 1 + Mathf.Max(0, s.chainBranches);    // T5 Twin Lash arcs to more neighbours

        // Breadth-first: each node arcs to its nearest un-hit mobs. With
        // chainBranches 0 this is the original single-path chain exactly.
        Queue<Mob> q = new Queue<Mob>();
        Queue<int> depths = new Queue<int>();
        Queue<int> parents = new Queue<int>();
        HashSet<Mob> seen = new HashSet<Mob>();
        q.Enqueue(target); depths.Enqueue(0); parents.Enqueue(-1); seen.Add(target);

        while (q.Count > 0 && hit.Count < maxTargets)
        {
            Mob cur = q.Dequeue();
            int depth = depths.Dequeue();
            int par = parents.Dequeue();
            if (cur == null || hit.Contains(cur)) continue;

            Vector3 cp = cur.transform.position + Vector3.up * 0.4f;
            int myIndex = pts.Count;
            pts.Add(cp);
            parent.Add(par);
            hit.Add(cur);

            // Sticky Sour (T6) ignores the 0.75^i falloff; Sour Power extends that to every chain.
            float dmg = (s.chainFullDamage || RogueMods.SourPower) ? s.damage : s.damage * Mathf.Pow(0.75f, depth);
            cur.TakeDamageFromTower(dmg, this);
            AddDamage(dmg);
            if (s.slowFactor > 0f) cur.ApplySlow(s.slowFactor, s.slowDuration);
            if (s.stunChance > 0f && Random.value < s.stunChance) cur.ApplyStun(s.stunDuration);

            int found = 0;
            while (found < fanout && hit.Count + q.Count < maxTargets)
            {
                Mob next = null;
                float best = s.chainRange;
                if (mobs != null)
                {
                    for (int j = 0; j < mobs.Count; j++)
                    {
                        Mob m = mobs[j];
                        if (m == null || seen.Contains(m)) continue;
                        float d = Vector3.Distance(cp, m.transform.position);
                        if (d <= best) { best = d; next = m; }
                    }
                }
                if (next == null) break;

                seen.Add(next);
                q.Enqueue(next);
                depths.Enqueue(depth + 1);
                parents.Enqueue(myIndex);
                found++;
            }
        }

        // Draw each arc from its actual source (root draws from the muzzle).
        for (int i = 0; i < pts.Count; i++)
            Tracer(parent[i] < 0 ? from : pts[parent[i]], pts[i],
                   new Color(0.95f, 1f, 0.10f), new Color(1f, 1f, 0.80f));
    }

    /// <summary>Pizza Oven T7 "Pizza Delivery": a direct hit that leaves a
    /// persistent damaging cheese zone at the impact point.</summary>
    void FirePizza(Mob target, TowerTierStats s)
    {
        if (target == null) return;

        target.TakeDamageFromTower(s.damage, this);
        AddDamage(s.damage);

        GameObject go = new GameObject("PizzaZone");
        go.transform.position = target.transform.position;

        Transform parent = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        if (parent != null) go.transform.SetParent(parent, true);

        PizzaZone z = go.AddComponent<PizzaZone>();
        z.Source = this;
        z.Radius = s.splashRadius;
        z.Dps = s.zoneDps;
        z.Duration = s.zoneDuration;
    }

    /// <summary>Fondue Fountain T7: a heavy molten-chocolate beam that also coats
    /// the target in stacking Dipped (damage-taken) stacks.</summary>
    void FireFondue(Vector3 from, Mob target, TowerTierStats s)
    {
        if (target == null) return;

        Vector3 hit = target.transform.position + Vector3.up * 0.4f;
        target.TakeDamageFromTower(s.damage, this);
        AddDamage(s.damage);
        if (s.dippedBonus > 0f)
            target.ApplyDipped(s.dippedBonus, s.dippedDuration, s.dippedMaxStacks);

        FxEvents.Beam(from, hit, new Color(0.45f, 0.25f, 0.10f));   // brown beam (mirrored)
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
            if (Vector3.Distance(transform.position, m.transform.position) <= EffRange(s))
                inRange.Add(m);
        }
        if (inRange.Count == 0) return;
        RankInRange(inRange);

        int shots = Mathf.Min((s.multiShot > 0 ? s.multiShot : 1) + (RogueMods.DoubleScoop && s.multiShot > 1 ? 1 : 0), inRange.Count);
        cooldown = s.fireInterval / (RogueMods.EffRate(BossWaveNow()) * SpeedMultiplier);
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);

        Vector3 muzzle = Muzzle();
        for (int i = 0; i < shots; i++)
            SpawnGumball(muzzle, inRange[i], s, GumColours[i % GumColours.Length]);

        if (Targeting == TowerTargeting.Random) randomTarget = null;   // re-roll next volley
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
        p.Source = this;
        p.Type = Type;
        p.Target = target;
        p.Speed = 14f;
        p.Damage = s.damage;               // Light impact at every tier; Candy Shell boosts T5+
        p.SlowFactor = s.slowFactor;
        p.SlowDuration = s.slowDuration;
        p.TarDamageBonus = s.tarDamageBonus;   // Sticky Tar (T6)
        p.TarLinger = s.tarLinger;
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
        p.Source = this;
        p.Type = Type;
        p.Target = target;
        p.Speed = s.projectileSpeed;
        p.Damage = s.damage;
        // Kettle Burst mini-splash: fall back to impactSplash when the tier has
        // no dedicated splashRadius.
        p.SplashRadius = s.splashRadius > 0f ? s.splashRadius : s.impactSplash;
        p.Bounces = s.bounceCount;
        p.BounceRange = s.bounceRange;
        p.PoisonDps = s.poisonDps;
        p.PoisonDuration = s.poisonDuration;
        p.PoisonMaxStacks = s.poisonMaxStacks;
        p.PoisonDetonateRadius = s.poisonDetonateRadius;
        p.PoisonDetonateFraction = s.poisonDetonateFraction;
        p.DippedBonus = s.dippedBonus;
        p.DippedDuration = s.dippedDuration;
        p.DippedMaxStacks = s.dippedMaxStacks;
        p.StunChance = s.stunChance;
        p.StunDuration = s.stunDuration;
        p.SplashSlowFactor = s.splashSlowFactor;
        p.SplashSlowDuration = s.splashSlowDuration;
        p.GoldPerHit = s.goldPerHit;
        p.Tint = TowerCatalog.Get(Type).color;
        p.Spin = Type != TowerType.Splash;   // the soda blob wobbles instead
    }

    /// <summary>SingleShot T6 "Kettle Burst": one pellet per in-range target,
    /// favouring whatever is furthest along the path.</summary>
    void SpawnVolley(Vector3 from, Mob target, TowerTierStats s)
    {
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        if (mobs == null) { SpawnProjectile(from, target, s); return; }

        List<Mob> inRange = new List<Mob>();
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null) continue;
            if (Vector3.Distance(transform.position, m.transform.position) <= EffRange(s))
                inRange.Add(m);
        }
        if (inRange.Count == 0) { SpawnProjectile(from, target, s); return; }
        inRange.Sort((a, b) => b.Progress.CompareTo(a.Progress));

        int shots = Mathf.Min(s.multiShot + (RogueMods.DoubleScoop && s.multiShot > 1 ? 1 : 0), inRange.Count);
        for (int i = 0; i < shots; i++)
            SpawnProjectile(from, inRange[i], s);
    }

    // A fast "bolt" of sour energy: a wide, flat ribbon that always faces the
    // camera, with a hot core down the middle.
    void Tracer(Vector3 a, Vector3 b, Color glow, Color core)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        FxEvents.Tracer(a, b, glow, core);   // mirror the bolt on remote boards

        Vector3 mid = a + dir * 0.5f;
        Vector3 fwd = dir / len;

        Vector3 toCam = Camera.main != null ? Camera.main.transform.position - mid : Vector3.up;
        Vector3 right = Vector3.Cross(fwd, toCam);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(fwd, Vector3.up);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, fwd).normalized;

        Quaternion rot = Quaternion.LookRotation(fwd, up);

        BoltPart("BoltGlow", mid, rot, new Vector3(0.190f, 0.050f, len), glow);
        BoltPart("BoltCore", mid, rot, new Vector3(0.075f, 0.022f, len * 1.01f), core);
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
