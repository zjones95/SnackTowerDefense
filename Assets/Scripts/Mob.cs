using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobDef Def;
    public float Health;
    public float MaxHealth;
    public float Progress;   // distance travelled along the path (for targeting)
    public ushort NetId;     // stable id for spectating
    public float DamageTaken; // damage recorded by an invincible mob (never applied to Health)

    private List<Vector3> path;
    private int pathIndex;
    private float slowTimer;
    private float speedMul = 1f;
    private float stunTimer;   // Chain T5 "Twin Lash"
    private float waveSpeed = 1f;
    private TDGameManager game;

    // Sticky Tar (Slow T6): bonus damage taken while slowed, lingering after.
    private float tarBonus;
    private float tarTimer;

    // Poison (Poison T5/T6): concurrent stacks, each with its own dps + timer + source tower.
    private class PoisonStack { public float dps; public float timer; public Tower source; }
    private readonly List<PoisonStack> poisonStacks = new List<PoisonStack>();
    private float poisonSourceDuration;
    private int poisonSourceMaxStacks;
    private Tower poisonSource;
    private float poisonDetonateRadius;
    private float poisonDetonateFraction;

    // Dipped (Fondue T7): concurrent damage-taken stacks, each with its own timer.
    private class DipStack { public float bonus; public float timer; }
    private readonly List<DipStack> dipStacks = new List<DipStack>();
    private float sourReduction, sourTimer, sourDuration, sourDeathRadius;
    private Tower sourSource;

    private Transform hpRoot;
    private Transform hpFill;
    private float barWidth = 0.9f;
    private float dashTimer;
    private float dashCooldown;
    private MobStatusIcons statusIcons;
    private bool lastSlowed, lastStunned, lastTarred, lastDipped;
    private int lastStacks;
    private float lastDipBonus;
    private float lastSourReduction;
    public float SourReduction => sourTimer > 0f ? sourReduction : 0f;

    /// <summary>Slowed (or stunned, which also stops movement).</summary>
    public bool IsSlowed => slowTimer > 0f || stunTimer > 0f;

    /// <summary>Held in place by a Chain T5 "Twin Lash" stun.</summary>
    public bool IsStunned => stunTimer > 0f;

    /// <summary>Coated in Slow T6 "Sticky Tar" (takes bonus damage while it lingers).</summary>
    public bool IsTarred => tarBonus > 0f && tarTimer > 0f;

    /// <summary>Number of concurrent poison stacks currently on the mob.</summary>
    public int PoisonStacks => poisonStacks.Count;

    /// <summary>Number of concurrent "Dipped" stacks currently on the mob.</summary>
    public int DipStacks => dipStacks.Count;

    /// <summary>Total dipped damage-taken bonus (for the status icon percentage).</summary>
    public float DipBonusTotal() { return DipTotal(); }

    public void Init(MobDef def, List<Vector3> waypoints, TDGameManager g, float healthMult, float speedMult)
    {
        Def = def;
        path = waypoints;
        game = g;
        // Bosses move 30% faster (enrage/dash stack on top, untouched).
        waveSpeed = speedMult * (def.archetype == MobArchetype.Boss ? 1.3f : 1f);
        MaxHealth = def.health * healthMult * TDBalance.MobHealthScale;
        Health = MaxHealth;
        pathIndex = 1;
        transform.position = path[0];

        hpRoot = MobVisual.Build(transform, def, out hpFill);
        barWidth = MobVisual.BarWidth;
        statusIcons = MobStatusIcons.Get(hpRoot);
        RefreshStatusIcons();
    }

    /// <summary>Push the current slow/poison state to the floating icons (cheap:
    /// only touches the GameObjects when something actually changed).</summary>
    void RefreshStatusIcons()
    {
        bool slowed = IsSlowed;
        bool stunned = IsStunned;
        int stacks = poisonStacks.Count;
        bool tarred = IsTarred;
        bool dipped = dipStacks.Count > 0;
        float dipBonus = dipped ? DipTotal() : 0f;
        if (slowed == lastSlowed && stunned == lastStunned && stacks == lastStacks && tarred == lastTarred &&
            dipped == lastDipped && Mathf.Abs(dipBonus - lastDipBonus) < 0.001f &&
            Mathf.Abs(SourReduction - lastSourReduction) < 0.001f) return;
        lastSlowed = slowed;
        lastStunned = stunned;
        lastStacks = stacks;
        lastTarred = tarred;
        lastDipped = dipped;
        lastDipBonus = dipBonus;
        lastSourReduction = SourReduction;
        if (statusIcons != null) statusIcons.Set(slowed, stunned, stacks > 0, stacks, tarred, dipped, dipBonus, SourReduction);
    }

    void Update()
    {
        if (path == null || pathIndex >= path.Count) return;

        RefreshStatusIcons();

        // boss traits
        if (Def.regen > 0f && Health < MaxHealth)
        {
            Health = Mathf.Min(MaxHealth, Health + Def.regen * Time.deltaTime);
            UpdateBar();
        }

        float enrage = Def.enrage > 0f
            ? 1f + Def.enrage * (1f - Mathf.Clamp01(Health / MaxHealth))
            : 1f;

        float dash = 1f;
        if (Def.dashEvery > 0f)
        {
            if (dashTimer > 0f)
            {
                dashTimer -= Time.deltaTime;
                dash = 2.6f;
            }
            else
            {
                dashCooldown -= Time.deltaTime;
                if (dashCooldown <= 0f) { dashTimer = 0.8f; dashCooldown = Def.dashEvery; }
            }
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) speedMul = 1f;
        }

        // tar lingers independently so it outlives the slow (Slow T6)
        if (tarTimer > 0f) tarTimer -= Time.deltaTime;
        if (sourTimer > 0f) sourTimer -= Time.deltaTime;

        if (poisonStacks.Count > 0)
        {
            for (int i = 0; i < poisonStacks.Count; i++)
            {
                float dmg = poisonStacks[i].dps * RogueMods.DamageMult() * Time.deltaTime;
                if (Def.invincible)
                    DamageTaken += dmg;   // recorded for scoring; health never drops
                else
                    Health -= dmg;
                if (poisonStacks[i].source != null) poisonStacks[i].source.AddDamage(dmg);
            }
            UpdateBar();
            if (!Def.invincible && Health <= 0f) { Die(); return; }

            for (int i = poisonStacks.Count - 1; i >= 0; i--)
                if ((poisonStacks[i].timer -= Time.deltaTime) <= 0f) poisonStacks.RemoveAt(i);
        }

        // Dipped (Fondue T7) just times out; the bonus is read in TakeDamage.
        if (dipStacks.Count > 0)
        {
            for (int i = dipStacks.Count - 1; i >= 0; i--)
                if ((dipStacks[i].timer -= Time.deltaTime) <= 0f) dipStacks.RemoveAt(i);
        }

        if (stunTimer > 0f) stunTimer -= Time.deltaTime;   // Chain T5 stun: stand still

        if (stunTimer <= 0f)
        {
            float speed = Def.speed * waveSpeed * speedMul * enrage * dash;
            Vector3 target = path[pathIndex];
            Vector3 pos = transform.position;
            Vector3 flat = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
            float dist = flat.magnitude;
            float step = speed * Time.deltaTime;

            if (dist <= step)
            {
                transform.position = new Vector3(target.x, 0f, target.z);
                Progress += dist;
                pathIndex++;
                if (pathIndex >= path.Count) { ReachEnd(); return; }
            }
            else
            {
                Vector3 dir = flat / dist;
                transform.position = pos + dir * step;
                Progress += step;
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // billboard the health bar toward the camera (also keeps fill/bg from z-fighting)
        if (hpRoot != null && Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.position - hpRoot.position;
            if (dir.sqrMagnitude > 0.001f) hpRoot.rotation = Quaternion.LookRotation(dir);
        }
    }

    public void ApplySlow(float removedFraction, float duration)
    {
        if (Def.slowResist >= 1f) return;   // a fully slow-immune boss
        removedFraction *= 0.5f;            // balance: slow strength halved across the board
        float mul = 1f - Mathf.Clamp01(removedFraction * RogueMods.SlowStrength * (1f - Def.slowResist));
        if (mul < speedMul) speedMul = mul;
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    /// <summary>Chain T5 "Twin Lash": briefly stops the mob. Stuns land on every
    /// mob, including slow-resistant bosses.</summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        stunTimer = Mathf.Max(stunTimer, duration);
    }

    /// <summary>Slow T6 "Sticky Tar": while slowed the mob takes
    /// <paramref name="bonus"/> extra damage from every source, lingering
    /// <paramref name="linger"/> seconds after the slow expires.</summary>
    public void ApplyTar(float bonus, float linger)
    {
        if (bonus <= 0f || Def.slowResist >= 1f) return;
        if (slowTimer <= 0f) return;   // tar only sticks when a slow actually lands
        tarBonus = Mathf.Max(tarBonus, bonus);
        tarTimer = Mathf.Max(tarTimer, slowTimer + Mathf.Max(0f, linger));
    }

    public void ApplySour(float reduction, float duration, Tower source, float deathRadius)
    {
        if (Def == null || reduction <= 0f || duration <= 0f) return;
        if (sourTimer <= 0f || reduction >= sourReduction)
        {
            sourReduction = Mathf.Clamp01(reduction);
            sourDuration = duration;
            sourDeathRadius = deathRadius;
            sourSource = source;
            sourTimer = Mathf.Max(sourTimer, duration);
        }
        RefreshStatusIcons();
    }

    /// <summary>Poison T5 "Extra Hot": up to <paramref name="maxStacks"/>
    /// concurrent stacks, each with its own duration. A maxStacks of 0/1 keeps
    /// the legacy single-stack (strongest-DPS) behaviour. T6 "Ghost Pepper"
    /// stores the detonation config to use when the mob dies.</summary>
    public void ApplyPoison(float dps, float duration, int maxStacks, Tower source,
                            float detonateRadius = 0f, float detonateFraction = 0f)
    {
        if (dps <= 0f || duration <= 0f) return;
        poisonSourceDuration = duration;
        poisonSourceMaxStacks = maxStacks;
        poisonSource = source;
        poisonDetonateRadius = detonateRadius;
        poisonDetonateFraction = detonateFraction;

        if (maxStacks <= 1)
        {
            if (poisonStacks.Count == 0) poisonStacks.Add(new PoisonStack());
            PoisonStack s = poisonStacks[0];
            if (dps > s.dps) s.dps = dps;
            s.timer = Mathf.Max(s.timer, duration);
            s.source = source;
            return;
        }

        if (poisonStacks.Count < maxStacks)
        {
            poisonStacks.Add(new PoisonStack { dps = dps, timer = duration, source = source });
            return;
        }

        // Full: refresh whichever stack is closest to expiring.
        int oldest = 0;
        for (int i = 1; i < poisonStacks.Count; i++)
            if (poisonStacks[i].timer < poisonStacks[oldest].timer) oldest = i;
        poisonStacks[oldest].dps = Mathf.Max(poisonStacks[oldest].dps, dps);
        poisonStacks[oldest].timer = Mathf.Max(poisonStacks[oldest].timer, duration);
        poisonStacks[oldest].source = source;
    }

    float CurrentPoisonDps()
    {
        float sum = 0f;
        for (int i = 0; i < poisonStacks.Count; i++) sum += poisonStacks[i].dps;
        return sum;
    }

    /// <summary>Fondue T7 "Dipped": up to <paramref name="maxStacks"/> concurrent
    /// damage-taken bonuses, each with its own duration. A maxStacks of 0/1 keeps
    /// the legacy single-stack behaviour. Does not scale with source stats: the
    /// applied bonus is whatever the tier configured.</summary>
    public void ApplyDipped(float bonus, float duration, int maxStacks)
    {
        if (bonus <= 0f || duration <= 0f) return;

        if (maxStacks <= 1)
        {
            if (dipStacks.Count == 0) dipStacks.Add(new DipStack());
            DipStack s = dipStacks[0];
            if (bonus > s.bonus) s.bonus = bonus;
            s.timer = Mathf.Max(s.timer, duration);
            return;
        }

        if (dipStacks.Count < maxStacks)
        {
            dipStacks.Add(new DipStack { bonus = bonus, timer = duration });
            return;
        }

        // Full: refresh whichever stack is closest to expiring.
        int oldest = 0;
        for (int i = 1; i < dipStacks.Count; i++)
            if (dipStacks[i].timer < dipStacks[oldest].timer) oldest = i;
        dipStacks[oldest].bonus = Mathf.Max(dipStacks[oldest].bonus, bonus);
        dipStacks[oldest].timer = Mathf.Max(dipStacks[oldest].timer, duration);
    }

    float DipTotal()
    {
        float sum = 0f;
        for (int i = 0; i < dipStacks.Count; i++) sum += dipStacks[i].bonus;
        return sum;
    }

    public void TakeDamage(float dmg)
    {
        TakeDamage(dmg, false, null);
    }

    /// <summary>Sniper T5 "Powdered Sour": crit damage that ignores flat armour.</summary>
    public void TakeDamageIgnoringArmour(float dmg)
    {
        TakeDamage(dmg, true, null);
    }

    /// <summary>Tower-originated damage: applies the local run's roguelike mods
    /// (global/crit/source-specific) before armour.</summary>
    public void TakeDamageFromTower(float dmg, Tower src)
    {
        TakeDamage(dmg, false, src);
    }

    public void TakeDamageFromTowerIgnoringArmour(float dmg, Tower src)
    {
        TakeDamage(dmg, true, src);
    }

    void TakeDamage(float dmg, bool ignoreArmour, Tower src)
    {
        if (dmg <= 0f || Health <= 0f) return;
        dmg *= RogueMods.DamageMult();
        if (src != null)
        {
            // All tower-originated damage benefits from Hot Sauce; the poison
            // stack tick above has its own damage path.
            dmg *= src.DamageMultiplier;
            if (RogueMods.Artillery && src.Type == TowerType.Sniper) dmg *= 1.3f;
            if (RogueMods.Caramelized && poisonStacks.Count > 0) dmg *= 1.25f;
            if (RogueMods.GiantSlayer && RogueMods.MaxTier > 0 && src.Tier == RogueMods.MaxTier)
                dmg += 0.02f * Health;
        }
        if (!ignoreArmour && RogueMods.CritChance > 0f && Random.value < RogueMods.CritChance)
            dmg *= RogueMods.CritMult;
        if (tarBonus > 0f && tarTimer > 0f) dmg *= 1f + tarBonus;   // tar hits every source
        if (dipStacks.Count > 0) dmg *= 1f + DipTotal();            // Fondue T7: dipped enemies take more
        if (!ignoreArmour)
            dmg = Mathf.Max(0f, dmg - Def.armour * (1f - SourReduction));
        if (dmg <= 0f) return;
        if (Def.invincible)
        {
            // Damage Test dummy: record the hit for scoring, but never wound it.
            DamageTaken += dmg;
            return;
        }
        Health -= dmg;
        UpdateBar();
        if (Health <= 0f) Die();
    }

    void UpdateBar()
    {
        if (hpFill == null) return;
        float f = Mathf.Clamp01(Health / MaxHealth);
        hpFill.localScale = new Vector3(barWidth * f, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3((barWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }

    void Die()
    {
        if (Def != null && Def.invincible) return;   // an invincible dummy can never die

        // Poison T6 "Ghost Pepper": a poisoned mob detonates on death, dealing a
        // fraction of its current poison DPS in a radius and re-applying poison
        // (which can chain into further detonations).
        if (poisonDetonateRadius > 0f && poisonDetonateFraction > 0f && poisonStacks.Count > 0)
            PoisonDetonate();

        if (SourReduction > 0f && sourDeathRadius > 0f && game != null)
        {
            foreach (Mob other in game.Mobs)
                if (other != null && other != this &&
                    Vector3.Distance(other.transform.position, transform.position) <= sourDeathRadius)
                    other.ApplySour(sourReduction, sourDuration, sourSource, sourDeathRadius);
        }

        if (TDAudio.Instance != null) TDAudio.Instance.Death();
        if (game != null) game.OnMobKilled(this);
        Destroy(gameObject);
    }

    void PoisonDetonate()
    {
        float dps = CurrentPoisonDps();
        float dmg = poisonDetonateFraction * dps;
        if (dmg <= 0f) return;

        var mobs = game != null ? game.Mobs : null;
        if (mobs == null) return;

        SplashFX.Spawn(transform.position + Vector3.up * 0.4f, poisonDetonateRadius, new Color(1f, 0.45f, 0.10f));

        // Snapshot the catch first: TakeDamage/ApplyPoison can kill and mutate
        // the live mob list while we iterate.
        List<Mob> caught = new List<Mob>();
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null || m == this) continue;
            if (Vector3.Distance(m.transform.position, transform.position) <= poisonDetonateRadius)
                caught.Add(m);
        }

        for (int i = 0; i < caught.Count; i++)
        {
            Mob m = caught[i];
            if (m == null) continue;
            m.ApplyPoison(dps, poisonSourceDuration, poisonSourceMaxStacks, poisonSource,
                          poisonDetonateRadius, poisonDetonateFraction);
            m.TakeDamageFromTower(dmg, poisonSource);
            if (poisonSource != null) poisonSource.AddDamage(dmg);
        }
    }

    void ReachEnd()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Leak();
        if (game != null) game.OnMobLeaked(this);
        Destroy(gameObject);
    }
}
