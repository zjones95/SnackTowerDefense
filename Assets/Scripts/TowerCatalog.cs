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
    public const int MaxTier = 3;
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

    static void EnsureInit()
    {
        if (defs != null) return;
        defs = new Dictionary<TowerType, TowerDef>();

        TowerDef d = new TowerDef();
        d.type = TowerType.SingleShot; d.displayName = "Popcorn Bucket"; d.color = new Color(0.95f, 0.80f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 3, range = 4.5f, fireInterval = 0.40f, projectileSpeed = 22f });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 5.5f, fireInterval = 0.325f, projectileSpeed = 24f });
        d.tiers.Add(new TowerTierStats { damage = 12, range = 6.5f, fireInterval = 0.25f, projectileSpeed = 26f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Splash; d.displayName = "Soda Cup"; d.color = new Color(0.40f, 0.70f, 1f);
        d.tiers.Add(new TowerTierStats { damage = 2, range = 4.0f, fireInterval = 0.60f, projectileSpeed = 16f, splashRadius = 1.6f });
        d.tiers.Add(new TowerTierStats { damage = 5, range = 4.8f, fireInterval = 0.525f, projectileSpeed = 18f, splashRadius = 2.1f });
        d.tiers.Add(new TowerTierStats { damage = 9, range = 5.6f, fireInterval = 0.45f, projectileSpeed = 20f, splashRadius = 2.7f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Slow; d.displayName = "Gumball Machine"; d.color = new Color(0.55f, 1f, 0.60f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 3.5f, fireInterval = 0.50f, slowFactor = 0.45f, slowDuration = 2.0f, multiShot = 2 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.2f, fireInterval = 0.50f, slowFactor = 0.55f, slowDuration = 2.2f, multiShot = 4 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.0f, fireInterval = 0.50f, slowFactor = 0.65f, slowDuration = 2.4f, multiShot = 6 });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Sniper; d.displayName = "Sour Straw"; d.color = new Color(0.90f, 0.50f, 0.95f);
        d.tiers.Add(new TowerTierStats { damage = 10, range = 9f, fireInterval = 0.90f });
        d.tiers.Add(new TowerTierStats { damage = 18, range = 11f, fireInterval = 0.80f });
        d.tiers.Add(new TowerTierStats { damage = 32, range = 13f, fireInterval = 0.70f });
        defs[d.type] = d;

        // --- new types ---
        d = new TowerDef();
        d.type = TowerType.Chain; d.displayName = "Sour Belt"; d.color = new Color(0.95f, 0.92f, 0.35f);
        d.tiers.Add(new TowerTierStats { damage = 2, range = 4.5f, fireInterval = 0.50f, chainCount = 2, chainRange = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 5, range = 5.2f, fireInterval = 0.45f, chainCount = 3, chainRange = 4.0f });
        d.tiers.Add(new TowerTierStats { damage = 7, range = 6.0f, fireInterval = 0.40f, chainCount = 4, chainRange = 4.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Pierce; d.displayName = "Skewer"; d.color = new Color(0.90f, 0.55f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 5, range = 7f, fireInterval = 0.65f, pierceCount = 3, pierceWidth = 1.1f });
        d.tiers.Add(new TowerTierStats { damage = 9, range = 8.5f, fireInterval = 0.60f, pierceCount = 4, pierceWidth = 1.3f });
        d.tiers.Add(new TowerTierStats { damage = 15, range = 10f, fireInterval = 0.55f, pierceCount = 5, pierceWidth = 1.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Poison; d.displayName = "Spicy Chips"; d.color = new Color(0.50f, 0.90f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 1, range = 4.5f, fireInterval = 0.50f, projectileSpeed = 20f, poisonDps = 6f, poisonDuration = 3f });
        d.tiers.Add(new TowerTierStats { damage = 2, range = 5.2f, fireInterval = 0.45f, projectileSpeed = 21f, poisonDps = 11f, poisonDuration = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 2, range = 6.0f, fireInterval = 0.40f, projectileSpeed = 22f, poisonDps = 18f, poisonDuration = 4f });
        defs[d.type] = d;

        // Gold is economy-only: no damage, a modest range, and a slow fire rate
        // that speeds up 25% per tier. Each confirmed hit pays goldPerHit.
        d = new TowerDef();
        d.type = TowerType.Gold; d.displayName = "Gold Coin"; d.color = new Color(1f, 0.82f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.5f, fireInterval = 5.0f, projectileSpeed = 20f, goldPerHit = 1 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.2f, fireInterval = 4.0f, projectileSpeed = 22f, goldPerHit = 2 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 6.0f, fireInterval = 3.2f, projectileSpeed = 24f, goldPerHit = 3 });
        defs[d.type] = d;
    }
}
