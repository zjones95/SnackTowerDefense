using System.Collections.Generic;
using UnityEngine;

// NOTE: append new types to the END only — TowerType is serialised as a byte in
// BoardSnapshot, so existing ordinals must never shift (network compatibility).
public enum TowerType { SingleShot, Splash, Slow, Sniper, Chain, Pierce, Poison, Gold }

[System.Serializable]
public class TowerTierStats
{
    public int damage = 10;
    public float range = 4f;
    public float fireInterval = 1f;
    public float projectileSpeed = 22f;

    public float splashRadius = 0f;
    public float slowFactor = 0f;      // fraction of speed removed
    public float slowDuration = 0f;
    public int multiShot = 0;          // Slow: gumballs fired per volley

    public int chainCount = 0;         // Chain: extra jumps after the first target
    public float chainRange = 0f;
    public int pierceCount = 0;        // Pierce: max targets along the line
    public float pierceWidth = 0f;
    public int bounceCount = 0;        // Bounce: extra hops
    public float bounceRange = 0f;
    public float poisonDps = 0f;       // Poison: damage per second
    public float poisonDuration = 0f;
    public int goldPerHit = 0;         // Gold: money awarded on each confirmed hit

    // ---- tier 4-6 schema (data only; behaviours land in a follow-up pass) ----
    // Defaults are neutral so tiers 1-3 are unaffected.

    // Sniper
    public float critChance = 0f;          // chance a hit crits (0-1)
    public float critMult = 1f;            // crit damage multiplier
    public bool critPierceArmour = false;  // crits ignore Mob.Def.armour
    public float deadeyeRamp = 0f;         // damage bonus per consecutive hit on one target
    public float deadeyeCap = 1f;          // cap on the deadeye bonus multiplier
    public float deadeyeCrit = 0f;         // extra crit chance granted per deadeye stack

    // Poison
    public int poisonMaxStacks = 0;        // max concurrent poison stacks (0/1 = no stacking)
    public float poisonDetonateRadius = 0f;    // on-death detonation radius
    public float poisonDetonateFraction = 0f;  // fraction of current DPS dealt on detonation

    // Chain
    public int chainBranches = 0;          // extra neighbours each node arcs to
    public bool chainFullDamage = false;   // jump damage ignores the 0.75^i falloff
    public float stunChance = 0f;          // Chain T5: chance to stun each enemy hit
    public float stunDuration = 0f;

    // Slow / tar
    public float tarDamageBonus = 0f;      // damage-taken bonus while slowed by this tower
    public float tarLinger = 0f;           // extra seconds the tar debuff lingers

    // Pierce
    public bool boomerangReturn = false;   // rod returns and skewers again at full damage

    // SingleShot
    public float impactSplash = 0f;        // small splash radius on every impact

    // Splash
    public float splashSlowFactor = 0f;    // slow applied to everything caught in the splash
    public float splashSlowDuration = 0f;
}

[System.Serializable]
public class TowerDef
{
    public TowerType type;
    public string displayName;
    public Color color;
    public List<TowerTierStats> tiers = new List<TowerTierStats>();

    public TowerTierStats Stats(int tier)
    {
        return tiers[Mathf.Clamp(tier - 1, 0, tiers.Count - 1)];
    }
}

// type -> per-tier stats. Add tiers by appending; add types by extending the
// enum and registering a def plus its SnackArt case.
//
// Balance passes:
//  - fire interval is halved and damage is halved vs. the original numbers, so
//    DPS is roughly unchanged while towers feel much more active.
//  - damage is reduced a further 20% (exact multiplier 0.80) across every tier
//    to make the run harder.
public static class TowerCatalog
{
    public const int MaxTier = 6;

    /// <summary>Highest source tier a 2:1 merge may consume. T3+T3 -> T4 is the
    /// top merge; T4+ and up advance by cash ascension instead. See TierPlan.md.</summary>
    public const int MaxMergeTier = 3;

    public const int BuildCost = 25;

    /// <summary>How many Gold towers a board may hold at once.</summary>
    public const int MaxGoldTowers = 4;

    private static Dictionary<TowerType, TowerDef> defs;

    /// <summary>Every tower type, including Gold (used by galleries).</summary>
    public static readonly TowerType[] AllTypes =
    {
        TowerType.SingleShot, TowerType.Splash, TowerType.Slow, TowerType.Sniper,
        TowerType.Chain, TowerType.Pierce, TowerType.Poison, TowerType.Gold
    };

    /// <summary>Types a random build / merge / re-roll may produce. Gold is
    /// excluded: it is only ever placed deliberately from Gold mode.</summary>
    public static readonly TowerType[] RandomTypes =
    {
        TowerType.SingleShot, TowerType.Splash, TowerType.Slow, TowerType.Sniper,
        TowerType.Chain, TowerType.Pierce, TowerType.Poison
    };

    public static TowerType RandomType() { return RandomTypes[Random.Range(0, RandomTypes.Length)]; }

    /// <summary>Random type that is guaranteed to differ from <paramref name="exclude"/>
    /// (used by re-roll so the result is visibly a change).</summary>
    public static TowerType RandomTypeExcluding(TowerType exclude)
    {
        if (RandomTypes.Length <= 1) return exclude;
        TowerType t;
        do { t = RandomType(); } while (t == exclude);
        return t;
    }

    public static TowerDef Get(TowerType t) { EnsureInit(); return defs[t]; }

    /// <summary>One-line description of a tower's unique tier-5 or tier-6
    /// modifier, or null when that tier has none (tiers 1-4, and all of Gold).</summary>
    public static string ModifierText(TowerType t, int tier)
    {
        if (t == TowerType.Gold) return null;

        if (tier == 5)
        {
            switch (t)
            {
                case TowerType.SingleShot: return "Ricochet Pop - shots bounce to 2 more enemies";
                case TowerType.Splash: return "Fizz Ricochet - bursts bounce once and detonate again";
                case TowerType.Slow: return "Candy Shell - gumballs deal impact damage";
                case TowerType.Sniper: return "Powdered Sour - 30% crit for x2.5 that ignores armour";
                case TowerType.Chain: return "Twin Lash - arcs branch to 2 more enemies, 5% chance to stun 1.5s";
                case TowerType.Pierce: return "Wide Skewer - a wider line that skewers more enemies";
                case TowerType.Poison: return "Extra Hot - poison stacks up to 3 times";
            }
        }
        else if (tier == 6)
        {
            switch (t)
            {
                case TowerType.SingleShot: return "Kettle Burst - 3-pellet volley, each impact mini-splashes";
                case TowerType.Splash: return "Sticky Soda - the splash also slows everything caught";
                case TowerType.Slow: return "Sticky Tar - slowed enemies take +30% damage from all towers";
                case TowerType.Sniper: return "Deadeye - damage ramps the longer it holds one target";
                case TowerType.Chain: return "Sticky Sour - jumps deal full damage and slow";
                case TowerType.Pierce: return "Boomerang Skewer - the rod returns and skewers again";
                case TowerType.Poison: return "Ghost Pepper - poisoned enemies explode on death";
            }
        }
        return null;
    }

    static void EnsureInit()
    {
        if (defs != null) return;
        defs = new Dictionary<TowerType, TowerDef>();

        TowerDef d = new TowerDef();
        d.type = TowerType.SingleShot; d.displayName = "Popcorn Bucket"; d.color = new Color(0.95f, 0.80f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 3, range = 4.5f, fireInterval = 0.40f, projectileSpeed = 22f });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 5.5f, fireInterval = 0.325f, projectileSpeed = 24f });
        d.tiers.Add(new TowerTierStats { damage = 12, range = 6.5f, fireInterval = 0.25f, projectileSpeed = 26f });
        // T4 pure stats; T5 Ricochet Pop (bounce 2 / 2.5); T6 Kettle Burst (3 pellets + mini-splash).
        d.tiers.Add(new TowerTierStats { damage = 16, range = 7.5f, fireInterval = 0.22f, projectileSpeed = 27f });
        d.tiers.Add(new TowerTierStats { damage = 24, range = 8.5f, fireInterval = 0.20f, projectileSpeed = 28f, bounceCount = 2, bounceRange = 2.5f });
        d.tiers.Add(new TowerTierStats { damage = 21, range = 9.5f, fireInterval = 0.20f, projectileSpeed = 29f, multiShot = 3, impactSplash = 1.3f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Splash; d.displayName = "Soda Cup"; d.color = new Color(0.40f, 0.70f, 1f);
        d.tiers.Add(new TowerTierStats { damage = 2, range = 4.0f, fireInterval = 0.60f, projectileSpeed = 16f, splashRadius = 1.6f });
        d.tiers.Add(new TowerTierStats { damage = 5, range = 4.8f, fireInterval = 0.525f, projectileSpeed = 18f, splashRadius = 2.1f });
        d.tiers.Add(new TowerTierStats { damage = 9, range = 5.6f, fireInterval = 0.45f, projectileSpeed = 20f, splashRadius = 2.7f });
        // T4 pure stats; T5 Fizz Ricochet (bounce 1 / 3.0); T6 Sticky Soda (splash slow 0.45 / 2.0).
        d.tiers.Add(new TowerTierStats { damage = 11, range = 6.4f, fireInterval = 0.40f, projectileSpeed = 22f, splashRadius = 3.3f });
        d.tiers.Add(new TowerTierStats { damage = 17, range = 7.2f, fireInterval = 0.36f, projectileSpeed = 24f, splashRadius = 3.9f, bounceCount = 1, bounceRange = 3.0f });
        d.tiers.Add(new TowerTierStats { damage = 24, range = 8.0f, fireInterval = 0.32f, projectileSpeed = 26f, splashRadius = 4.4f, splashSlowFactor = 0.45f, splashSlowDuration = 2.0f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Slow; d.displayName = "Gumball Machine"; d.color = new Color(0.55f, 1f, 0.60f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 3.5f, fireInterval = 0.50f, slowFactor = 0.45f, slowDuration = 2.0f, multiShot = 2 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.2f, fireInterval = 0.50f, slowFactor = 0.55f, slowDuration = 2.2f, multiShot = 4 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.0f, fireInterval = 0.50f, slowFactor = 0.65f, slowDuration = 2.4f, multiShot = 6 });
        // T4 pure stats; T5 Candy Shell (impact damage 3); T6 Sticky Tar (impact 6 + tar +30% / 1.5s).
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.8f, fireInterval = 0.48f, slowFactor = 0.72f, slowDuration = 2.6f, multiShot = 7 });
        d.tiers.Add(new TowerTierStats { damage = 3, range = 6.6f, fireInterval = 0.46f, slowFactor = 0.78f, slowDuration = 2.8f, multiShot = 8 });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 7.4f, fireInterval = 0.44f, slowFactor = 0.84f, slowDuration = 3.0f, multiShot = 10, tarDamageBonus = 0.30f, tarLinger = 1.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Sniper; d.displayName = "Sour Straw"; d.color = new Color(0.90f, 0.50f, 0.95f);
        d.tiers.Add(new TowerTierStats { damage = 10, range = 6.75f, fireInterval = 0.90f });
        d.tiers.Add(new TowerTierStats { damage = 18, range = 8.25f, fireInterval = 0.80f });
        d.tiers.Add(new TowerTierStats { damage = 32, range = 9.75f, fireInterval = 0.70f });
        // T4 pure stats; T5 Powdered Sour (30% crit x2.5, pierces armour);
        // T6 Deadeye (+15%/stack cap x1.8, +5% crit/stack) on top of the crit.
        d.tiers.Add(new TowerTierStats { damage = 38, range = 11.25f, fireInterval = 0.62f });
        d.tiers.Add(new TowerTierStats { damage = 54, range = 12.75f, fireInterval = 0.56f, critChance = 0.30f, critMult = 2.5f, critPierceArmour = true });
        d.tiers.Add(new TowerTierStats { damage = 77, range = 14.25f, fireInterval = 0.50f, critChance = 0.30f, critMult = 2.5f, critPierceArmour = true, deadeyeRamp = 0.15f, deadeyeCap = 1.8f, deadeyeCrit = 0.05f });
        defs[d.type] = d;

        // --- new types ---
        d = new TowerDef();
        d.type = TowerType.Chain; d.displayName = "Sour Belt"; d.color = new Color(0.95f, 0.92f, 0.35f);
        d.tiers.Add(new TowerTierStats { damage = 2, range = 4.5f, fireInterval = 0.50f, chainCount = 2, chainRange = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 5, range = 5.2f, fireInterval = 0.45f, chainCount = 3, chainRange = 4.0f });
        d.tiers.Add(new TowerTierStats { damage = 7, range = 6.0f, fireInterval = 0.40f, chainCount = 4, chainRange = 4.5f });
        // T4 pure stats; T5 Twin Lash (branches 2); T6 Sticky Sour (full damage, slow 0.40 / 2.0, +1 jump).
        d.tiers.Add(new TowerTierStats { damage = 11, range = 6.8f, fireInterval = 0.36f, chainCount = 5, chainRange = 5.0f });
        d.tiers.Add(new TowerTierStats { damage = 16, range = 7.6f, fireInterval = 0.34f, chainCount = 6, chainRange = 5.5f, chainBranches = 2, stunChance = 0.05f, stunDuration = 1.5f });
        d.tiers.Add(new TowerTierStats { damage = 22, range = 8.4f, fireInterval = 0.32f, chainCount = 7, chainRange = 6.0f, chainFullDamage = true, slowFactor = 0.40f, slowDuration = 2.0f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Pierce; d.displayName = "Skewer"; d.color = new Color(0.90f, 0.55f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 5, range = 7f, fireInterval = 0.65f, pierceCount = 3, pierceWidth = 1.1f });
        d.tiers.Add(new TowerTierStats { damage = 9, range = 8.5f, fireInterval = 0.60f, pierceCount = 4, pierceWidth = 1.3f });
        d.tiers.Add(new TowerTierStats { damage = 15, range = 10f, fireInterval = 0.55f, pierceCount = 5, pierceWidth = 1.5f });
        // T4 pure stats; T5 Wide Skewer (pc 8 / pw 2.0); T6 Boomerang Skewer (pc 9 / pw 2.2, returns).
        d.tiers.Add(new TowerTierStats { damage = 22, range = 11.5f, fireInterval = 0.50f, pierceCount = 6, pierceWidth = 1.7f });
        d.tiers.Add(new TowerTierStats { damage = 32, range = 13f, fireInterval = 0.46f, pierceCount = 8, pierceWidth = 2.0f });
        d.tiers.Add(new TowerTierStats { damage = 45, range = 14.5f, fireInterval = 0.42f, pierceCount = 9, pierceWidth = 2.2f, boomerangReturn = true });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Poison; d.displayName = "Spicy Chips"; d.color = new Color(0.50f, 0.90f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 1, range = 4.5f, fireInterval = 0.50f, projectileSpeed = 20f, poisonDps = 6f, poisonDuration = 3f });
        d.tiers.Add(new TowerTierStats { damage = 2, range = 5.2f, fireInterval = 0.45f, projectileSpeed = 21f, poisonDps = 11f, poisonDuration = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 2, range = 6.0f, fireInterval = 0.40f, projectileSpeed = 22f, poisonDps = 18f, poisonDuration = 4f });
        // T4 pure stats; T5 Extra Hot (up to 3 poison stacks);
        // T6 Ghost Pepper (death detonation: full DPS in 2.5 radius, re-applies poison).
        d.tiers.Add(new TowerTierStats { damage = 3, range = 6.8f, fireInterval = 0.36f, projectileSpeed = 23f, poisonDps = 24f, poisonDuration = 4.5f });
        d.tiers.Add(new TowerTierStats { damage = 4, range = 7.6f, fireInterval = 0.34f, projectileSpeed = 24f, poisonDps = 33f, poisonDuration = 5.0f, poisonMaxStacks = 3 });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 8.4f, fireInterval = 0.32f, projectileSpeed = 25f, poisonDps = 45f, poisonDuration = 5.5f, poisonDetonateRadius = 3.0f, poisonDetonateFraction = 2f });
        defs[d.type] = d;

        // Gold is economy-only: no damage, a modest range, and a slow fire rate
        // that speeds up ~25% per tier. The payout is a flat $1 per confirmed
        // hit at every tier - the faster rate is the whole upgrade.
        d = new TowerDef();
        d.type = TowerType.Gold; d.displayName = "Gold Coin"; d.color = new Color(1f, 0.82f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.5f, fireInterval = 5.0f, projectileSpeed = 20f, goldPerHit = 1 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.2f, fireInterval = 4.0f, projectileSpeed = 22f, goldPerHit = 2 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 6.0f, fireInterval = 3.2f, projectileSpeed = 24f, goldPerHit = 3 });
        defs[d.type] = d;
    }
}
