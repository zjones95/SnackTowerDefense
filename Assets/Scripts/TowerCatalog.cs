using System.Collections.Generic;
using UnityEngine;

public enum TowerType { SingleShot, Splash, Slow, Sniper, Chain, Pierce, Poison }

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
// Balance pass: fire interval is halved and damage is halved vs. the original
// numbers, so DPS is roughly unchanged while towers feel much more active.
public static class TowerCatalog
{
    public const int MaxTier = 3;
    public const int BuildCost = 25;

    private static Dictionary<TowerType, TowerDef> defs;

    public static readonly TowerType[] AllTypes =
    {
        TowerType.SingleShot, TowerType.Splash, TowerType.Slow, TowerType.Sniper,
        TowerType.Chain, TowerType.Pierce, TowerType.Poison
    };

    public static TowerType RandomType() { return AllTypes[Random.Range(0, AllTypes.Length)]; }

    public static TowerDef Get(TowerType t) { EnsureInit(); return defs[t]; }

    static void EnsureInit()
    {
        if (defs != null) return;
        defs = new Dictionary<TowerType, TowerDef>();

        TowerDef d = new TowerDef();
        d.type = TowerType.SingleShot; d.displayName = "Popcorn Bucket"; d.color = new Color(0.95f, 0.80f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 4, range = 4.5f, fireInterval = 0.40f, projectileSpeed = 22f });
        d.tiers.Add(new TowerTierStats { damage = 8, range = 5.5f, fireInterval = 0.325f, projectileSpeed = 24f });
        d.tiers.Add(new TowerTierStats { damage = 15, range = 6.5f, fireInterval = 0.25f, projectileSpeed = 26f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Splash; d.displayName = "Soda Cup"; d.color = new Color(0.40f, 0.70f, 1f);
        d.tiers.Add(new TowerTierStats { damage = 3, range = 4.0f, fireInterval = 0.60f, projectileSpeed = 16f, splashRadius = 1.6f });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 4.8f, fireInterval = 0.525f, projectileSpeed = 18f, splashRadius = 2.1f });
        d.tiers.Add(new TowerTierStats { damage = 11, range = 5.6f, fireInterval = 0.45f, projectileSpeed = 20f, splashRadius = 2.7f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Slow; d.displayName = "Gumball Machine"; d.color = new Color(0.55f, 1f, 0.60f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 3.5f, fireInterval = 0.50f, slowFactor = 0.45f, slowDuration = 2.0f, multiShot = 2 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.2f, fireInterval = 0.50f, slowFactor = 0.55f, slowDuration = 2.2f, multiShot = 4 });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.0f, fireInterval = 0.50f, slowFactor = 0.65f, slowDuration = 2.4f, multiShot = 6 });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Sniper; d.displayName = "Sour Straw"; d.color = new Color(0.90f, 0.50f, 0.95f);
        d.tiers.Add(new TowerTierStats { damage = 13, range = 9f, fireInterval = 0.90f });
        d.tiers.Add(new TowerTierStats { damage = 23, range = 11f, fireInterval = 0.80f });
        d.tiers.Add(new TowerTierStats { damage = 40, range = 13f, fireInterval = 0.70f });
        defs[d.type] = d;

        // --- new types ---
        d = new TowerDef();
        d.type = TowerType.Chain; d.displayName = "Sour Belt"; d.color = new Color(0.95f, 0.92f, 0.35f);
        d.tiers.Add(new TowerTierStats { damage = 3, range = 4.5f, fireInterval = 0.50f, chainCount = 2, chainRange = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 6, range = 5.2f, fireInterval = 0.45f, chainCount = 3, chainRange = 4.0f });
        d.tiers.Add(new TowerTierStats { damage = 9, range = 6.0f, fireInterval = 0.40f, chainCount = 4, chainRange = 4.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Pierce; d.displayName = "Skewer"; d.color = new Color(0.90f, 0.55f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 6, range = 7f, fireInterval = 0.65f, pierceCount = 3, pierceWidth = 1.1f });
        d.tiers.Add(new TowerTierStats { damage = 11, range = 8.5f, fireInterval = 0.60f, pierceCount = 4, pierceWidth = 1.3f });
        d.tiers.Add(new TowerTierStats { damage = 19, range = 10f, fireInterval = 0.55f, pierceCount = 5, pierceWidth = 1.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Poison; d.displayName = "Spicy Chips"; d.color = new Color(0.50f, 0.90f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 1, range = 4.5f, fireInterval = 0.50f, projectileSpeed = 20f, poisonDps = 6f, poisonDuration = 3f });
        d.tiers.Add(new TowerTierStats { damage = 2, range = 5.2f, fireInterval = 0.45f, projectileSpeed = 21f, poisonDps = 11f, poisonDuration = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 3, range = 6.0f, fireInterval = 0.40f, projectileSpeed = 22f, poisonDps = 18f, poisonDuration = 4f });
        defs[d.type] = d;
    }
}
