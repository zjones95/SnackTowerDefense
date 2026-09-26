using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour
{
    public MobDef Def;
    public float Health;
    public float MaxHealth;
    public float Progress;   // distance travelled along the path (for targeting)
    public ushort NetId;     // stable id for spectating

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

    // Poison (Poison T5/T6): concurrent stacks, each with its own dps + timer.
    private class PoisonStack { public float dps; public float timer; }
    private readonly List<PoisonStack> poisonStacks = new List<PoisonStack>();
    private float poisonSourceDuration;
    private int poisonSourceMaxStacks;
    private float poisonDetonateRadius;
    private float poisonDetonateFraction;

    private Transform hpRoot;
    private Transform hpFill;
    private float barWidth = 0.9f;
    private float dashTimer;
    private float dashCooldown;

    public void Init(MobDef def, List<Vector3> waypoints, TDGameManager g, float healthMult, float speedMult)
    {
        Def = def;
        path = waypoints;
        game = g;
        waveSpeed = speedMult;
        MaxHealth = def.health * healthMult * TDBalance.MobHealthScale;
        Health = MaxHealth;
        pathIndex = 1;
        transform.position = path[0];

        hpRoot = MobVisual.Build(transform, def, out hpFill);
        barWidth = MobVisual.BarWidth;
    }

    void Update()
    {
        if (path == null || pathIndex >= path.Count) return;

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

        if (poisonStacks.Count > 0)
        {
            Health -= CurrentPoisonDps() * Time.deltaTime;
            UpdateBar();
            if (Health <= 0f) { Die(); return; }

            for (int i = poisonStacks.Count - 1; i >= 0; i--)
                if ((poisonStacks[i].timer -= Time.deltaTime) <= 0f) poisonStacks.RemoveAt(i);
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
        if (Def.slowImmune) return;   // e.g. Coconut and the Granola Mom
        float mul = 1f - Mathf.Clamp01(removedFraction);
        if (mul < speedMul) speedMul = mul;
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    /// <summary>Chain T5 "Twin Lash": briefly stops the mob. Slow-immune bosses resist.</summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f || Def.slowImmune) return;
        stunTimer = Mathf.Max(stunTimer, duration);
    }

    /// <summary>Slow T6 "Sticky Tar": while slowed the mob takes
    /// <paramref name="bonus"/> extra damage from every source, lingering
    /// <paramref name="linger"/> seconds after the slow expires.</summary>
    public void ApplyTar(float bonus, float linger)
    {
        if (bonus <= 0f || Def.slowImmune) return;
        if (slowTimer <= 0f) return;   // tar only sticks when a slow actually lands
        tarBonus = Mathf.Max(tarBonus, bonus);
        tarTimer = Mathf.Max(tarTimer, slowTimer + Mathf.Max(0f, linger));
    }

    /// <summary>Poison T5 "Extra Hot": up to <paramref name="maxStacks"/>
    /// concurrent stacks, each with its own duration. A maxStacks of 0/1 keeps
    /// the legacy single-stack (strongest-DPS) behaviour. T6 "Ghost Pepper"
    /// stores the detonation config to use when the mob dies.</summary>
    public void ApplyPoison(float dps, float duration, int maxStacks = 0,
                            float detonateRadius = 0f, float detonateFraction = 0f)
    {
        if (dps <= 0f || duration <= 0f) return;
        poisonSourceDuration = duration;
        poisonSourceMaxStacks = maxStacks;
        poisonDetonateRadius = detonateRadius;
        poisonDetonateFraction = detonateFraction;

        if (maxStacks <= 1)
        {
            if (poisonStacks.Count == 0) poisonStacks.Add(new PoisonStack());
            PoisonStack s = poisonStacks[0];
            if (dps > s.dps) s.dps = dps;
            s.timer = Mathf.Max(s.timer, duration);
            return;
        }

        if (poisonStacks.Count < maxStacks)
        {
            poisonStacks.Add(new PoisonStack { dps = dps, timer = duration });
            return;
        }

        // Full: refresh whichever stack is closest to expiring.
        int oldest = 0;
        for (int i = 1; i < poisonStacks.Count; i++)
            if (poisonStacks[i].timer < poisonStacks[oldest].timer) oldest = i;
        poisonStacks[oldest].dps = Mathf.Max(poisonStacks[oldest].dps, dps);
        poisonStacks[oldest].timer = Mathf.Max(poisonStacks[oldest].timer, duration);
    }

    float CurrentPoisonDps()
    {
        float sum = 0f;
        for (int i = 0; i < poisonStacks.Count; i++) sum += poisonStacks[i].dps;
        return sum;
    }

    public void TakeDamage(float dmg)
    {
        TakeDamage(dmg, false);
    }

    /// <summary>Sniper T5 "Powdered Sour": crit damage that ignores flat armour.</summary>
    public void TakeDamageIgnoringArmour(float dmg)
    {
        TakeDamage(dmg, true);
    }

    void TakeDamage(float dmg, bool ignoreArmour)
    {
        if (dmg <= 0f || Health <= 0f) return;
        if (tarBonus > 0f && tarTimer > 0f) dmg *= 1f + tarBonus;   // tar hits every source
        if (!ignoreArmour)
            dmg = Mathf.Max(0f, dmg - Def.armour);   // armoured bosses shrug off flat damage
        if (dmg <= 0f) return;
        Health -= dmg;
        UpdateBar();
        if (Health <= 0f) Die();
    }

    void UpdateBar()
    {
        if (hpFill == null) return;
        float f = Mathf.Clamp01(Health / MaxHealth);
        hpFill.localScale = new Vector3(barWidth * f, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3(-(barWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }

    void Die()
    {
        // Poison T6 "Ghost Pepper": a poisoned mob detonates on death, dealing a
        // fraction of its current poison DPS in a radius and re-applying poison
        // (which can chain into further detonations).
        if (poisonDetonateRadius > 0f && poisonDetonateFraction > 0f && poisonStacks.Count > 0)
            PoisonDetonate();

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

        SplashFX.Spawn(transform.position + Vector3.up * 0.4f, poisonDetonateRadius, new Color(0.55f, 0.95f, 0.25f));

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
            m.ApplyPoison(dps, poisonSourceDuration, poisonSourceMaxStacks,
                          poisonDetonateRadius, poisonDetonateFraction);
            m.TakeDamage(dmg);
        }
    }

    void ReachEnd()
    {
        if (TDAudio.Instance != null) TDAudio.Instance.Leak();
        if (game != null) game.OnMobLeaked(this);
        Destroy(gameObject);
    }
}
