using System.Collections.Generic;
using UnityEngine;

/// <summary>Passive support auras and the three new snack attack patterns.</summary>
public partial class Tower
{
    private Transform tierLabel;
    private TextMesh buffLabel;
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
        // The same floating billboard as the tier digit keeps the two buffs aligned.
        if (tierLabel == null) return;
        if (dmg <= 0f && speed <= 0f)
        {
            if (buffLabel != null) buffLabel.gameObject.SetActive(false);
            return;
        }
        if (buffLabel == null)
        {
            GameObject go = new GameObject("BuffBadges");
            go.transform.SetParent(tierLabel, false);
            go.transform.localPosition = new Vector3(0f, .36f, -.005f);
            buffLabel = go.AddComponent<TextMesh>();
            buffLabel.characterSize = .025f;
            buffLabel.fontSize = 100;
            buffLabel.anchor = TextAnchor.MiddleCenter;
            buffLabel.alignment = TextAlignment.Center;
            buffLabel.color = Color.white;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                buffLabel.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }
        if (!buffLabel.gameObject.activeSelf) buffLabel.gameObject.SetActive(true);
        string label = (dmg > 0f ? "\u25c6 +" + Mathf.RoundToInt(dmg * 100f) + "% DMG" : "") +
            (dmg > 0f && speed > 0f ? "\n" : "") +
            (speed > 0f ? "\u2615 +" + Mathf.RoundToInt(speed * 100f) + "% SPD" : "");
        if (buffLabel.text != label) buffLabel.text = label;
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
            FireCrumbFan(target, s);
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

    void FireCrumbFan(Mob target, TowerTierStats s)
    {
        Vector3 forward = target.transform.position - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < .001f) forward = transform.forward;
        forward.Normalize();
        int count = s.crumbCount + (Tier >= 6 ? crumbRamp : 0);
        var mobs = TDGameManager.Instance != null ? TDGameManager.Instance.Mobs : null;
        Vector3 from = Muzzle();
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : (i / (float)(count - 1) - .5f) * s.crumbCone;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
            Mob hit = null;
            float nearest = s.range;
            if (mobs != null) foreach (Mob m in mobs)
            {
                if (m == null) continue;
                Vector3 offset = m.transform.position - transform.position;
                offset.y = 0f;
                float along = Vector3.Dot(offset, dir);
                if (along < 0f || along > nearest) continue;
                if ((offset - dir * along).sqrMagnitude > .32f * .32f) continue;
                nearest = along; hit = m;
            }
            if (hit != null)
            {
                hit.TakeDamageFromTower(s.damage, this);
                AddDamage(s.damage);
            }
            // A sparse visible sample communicates the full cone without creating
            // dozens of GameObjects every .23 seconds.
            if (i % 3 == 0) Tracer(from, from + dir * Mathf.Min(nearest, s.range),
                new Color(.54f, .29f, .11f), new Color(.92f, .69f, .33f));
        }
    }
}
