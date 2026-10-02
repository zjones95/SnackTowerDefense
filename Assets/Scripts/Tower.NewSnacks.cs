using System.Collections.Generic;
using UnityEngine;

/// <summary>Passive support auras and the three new snack attack patterns.</summary>
public partial class Tower
{
    private Transform tierLabel;
    private Transform buffRoot;
    private GameObject swordBadge, boltBadge;
    private OutlinedText swordPct, boltPct;
    private float damageBuff, speedBuff;
    private int toastShot, toastStacks;
    private float lastSnackShot = -99f;
    private int crumbRamp;
    private float crumbActiveTime;

    public float DamageMultiplier { get { return 1f + damageBuff; } }
    private float SpeedMultiplier { get { return 1f + speedBuff; } }
    public float DamageBuff { get { return damageBuff; } }
    public float SpeedBuff { get { return speedBuff; } }

    /// <summary>Only the strongest aura of each kind applies. T6 counts other
    /// same-type towers once, regardless of whether their own auras overlap.</summary>
    void UpdateSnackBuffs()
    {
        float dmg = 0f, speed = 0f;
        TDGameManager gm = TDGameManager.Instance;
        if (gm != null) CalculateSnackBuffs(this, gm.AllTowers, out dmg, out speed);
        damageBuff = dmg;
        speedBuff = speed;

        if (tierLabel == null) return;
        bool showDmg = dmg > 0f, showSpd = speed > 0f;
        if (!showDmg && !showSpd)
        {
            if (buffRoot != null) buffRoot.gameObject.SetActive(false);
            return;
        }
        EnsureBuffBadges();
        if (!buffRoot.gameObject.activeSelf) buffRoot.gameObject.SetActive(true);

        // Sword (damage) and lightning (attack speed) sit side by side above the
        // tier digit; a lone badge is centred. Their percentages are overlaid.
        float off = showDmg && showSpd ? .11f : 0f;
        SetBuffBadge(swordBadge, swordPct, showDmg, showDmg && showSpd ? -off : 0f, dmg);
        SetBuffBadge(boltBadge, boltPct, showSpd, showSpd && showDmg ? off : 0f, speed);
    }

    void EnsureBuffBadges()
    {
        if (buffRoot != null) return;
        buffRoot = new GameObject("BuffBadges").transform;
        buffRoot.SetParent(tierLabel, false);
        buffRoot.localPosition = new Vector3(0f, .34f, 0f);
        swordBadge = MakeBuffBadge("DmgBuff", TDTextures.IconSword(), out swordPct);
        boltBadge = MakeBuffBadge("SpdBuff", TDTextures.IconLightning(), out boltPct);
    }

    GameObject MakeBuffBadge(string name, Texture2D tex, out OutlinedText pct)
    {
        GameObject icon = TDVisuals.Quad(buffRoot, name, Vector3.zero, .19f, MobVisual.IconMaterial(tex));
        Renderer r = icon.GetComponent<Renderer>();
        if (r != null)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        pct = OutlinedText.Build(icon.transform, "0%", .014f, Color.white, new Vector3(0f, 0f, -.012f));
        return icon;
    }

    static void SetBuffBadge(GameObject badge, OutlinedText pct, bool show, float x, float value)
    {
        if (badge == null) return;
        if (badge.activeSelf != show) badge.SetActive(show);
        if (!show) return;
        badge.transform.localPosition = new Vector3(x, 0f, 0f);
        string text = Mathf.RoundToInt(value * 100f) + "%";
        if (pct != null) pct.Text = text;
    }

    public static void CalculateSnackBuffs(Tower receiver, IEnumerable<Tower> towers, out float damage, out float speed)
    {
        damage = 0f; speed = 0f;
        foreach (Tower t in towers)
        {
            if (t == null || t == receiver) continue;
            if (t.Type != TowerType.HotSauce && t.Type != TowerType.CoffeeMug) continue;
            TowerTierStats ts = t.Stats;
            if (Vector3.Distance(receiver.transform.position, t.transform.position) > ts.auraRadius) continue;
            float amount = ts.auraBonus;
            if (t.Tier == 6)
            {
                int others = 0;
                foreach (Tower other in towers)
                    if (other != null && other != t && other.Type == t.Type) others++;
                amount += Mathf.Min(5, others) * .01f;
            }
            if (t.Type == TowerType.HotSauce) damage = Mathf.Max(damage, amount);
            else speed = Mathf.Max(speed, amount);
        }
    }

    void FireNewSnackTower(Mob target, TowerTierStats s)
    {
        if (Time.time - lastSnackShot >= 5f) { toastStacks = 0; crumbRamp = 0; crumbActiveTime = 0f; }
        float rate = RogueMods.EffRate(BossWaveNow()) * SpeedMultiplier;
        if (Type == TowerType.PopTartToaster)
        {
            // The fourth shot is the big one; all four advance Breakfast Rush.
            bool big = toastShot == 3;
            SpawnSnackProjectile(target, big ? s.damage : s.toastSmallDamage,
                big ? s.splashRadius : 0f, big ? .38f : .20f, s);
            toastShot = (toastShot + 1) % 4;
            if (Tier >= 6) toastStacks = Mathf.Min(100, toastStacks + 1);
            float ramp = 1f + toastStacks * .01f;
            cooldown = (big ? s.fireInterval - .54f : .18f) / (rate * ramp);
        }
        else if (Type == TowerType.CookieCrumbler)
        {
            if (Tier >= 6 && lastSnackShot > 0f)
            {
                crumbActiveTime += Time.time - lastSnackShot;
                crumbRamp = Mathf.Min(8, Mathf.FloorToInt(crumbActiveTime));
            }
            SpawnCrumbFan(target, s);
            cooldown = s.fireInterval / rate;
        }
        else
        {
            SpawnSnackProjectile(target, 0f, 0f, .20f, s);
            cooldown = s.fireInterval / rate;
        }
        lastSnackShot = Time.time;
        if (TDAudio.Instance != null) TDAudio.Instance.Shot(Type);
    }

    void SpawnSnackProjectile(Mob target, float damage, float splash, float size, TowerTierStats s)
    {
        GameObject go = GameObject.CreatePrimitive(Type == TowerType.SourFizz ? PrimitiveType.Sphere : PrimitiveType.Cube);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        go.name = Type == TowerType.SourFizz ? "SourBubble" : "PopTart";
        go.transform.position = Muzzle();
        go.transform.localScale = Type == TowerType.SourFizz ? Vector3.one * size : new Vector3(size, size * .15f, size * .85f);
        go.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(Type == TowerType.SourFizz
            ? new Color(.95f, .4f, .72f) : new Color(1f, .83f, .61f), 0f, .55f);
        if (TDGameManager.Instance != null && TDGameManager.Instance.ProjectilesRoot != null)
            go.transform.SetParent(TDGameManager.Instance.ProjectilesRoot, true);
        Projectile p = go.AddComponent<Projectile>();
        p.Source = this; p.Type = Type; p.Target = target;
        p.Speed = s.projectileSpeed; p.Damage = damage; p.SplashRadius = splash;
        p.SourReduction = s.sourArmourReduction; p.SourDuration = s.sourDuration;
        p.SourSpreadRadius = s.sourSpreadRadius; p.SourDeathRadius = Tier >= 6 ? 2f : 0f;
        p.ZoneDps = splash > 0f ? s.zoneDps : 0f;
        p.ZoneDuration = splash > 0f ? s.zoneDuration : 0f;
        p.Tint = TowerCatalog.Get(Type).color;
    }

    void SpawnCrumbFan(Mob target, TowerTierStats s)
    {
        Vector3 forward = target.transform.position - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < .001f) forward = transform.forward;
        forward.Normalize();

        // Every bit leaves from the same packed point, then the volley fans out
        // into a cone as the pieces fly.
        int count = s.crumbCount + (Tier >= 6 ? crumbRamp : 0);
        Vector3 from = Muzzle();
        Transform root = TDGameManager.Instance != null ? TDGameManager.Instance.ProjectilesRoot : null;
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : (i / (float)(count - 1) - .5f) * s.crumbCone;
            angle += Random.Range(-3f, 3f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.name = "CookieBit";
            go.transform.position = from;
            go.transform.localScale = Vector3.one * .13f;
            go.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(.62f, .38f, .18f), 0f, .35f);
            if (root != null) go.transform.SetParent(root, true);

            CrumbProjectile c = go.AddComponent<CrumbProjectile>();
            c.Direction = dir;
            c.Speed = 13f;
            c.Range = s.range;
            c.Damage = s.damage;
            c.Source = this;
        }
    }
}
