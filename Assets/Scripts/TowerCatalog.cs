using System.Collections.Generic;
using UnityEngine;

public enum TowerType { SingleShot, Splash, Slow, Sniper, Chain, Pierce, Bounce, Poison }

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
public static class TowerCatalog
{
    public const int MaxTier = 3;
    public const int BuildCost = 25;

    private static Dictionary<TowerType, TowerDef> defs;

    public static readonly TowerType[] AllTypes =
    {
        TowerType.SingleShot, TowerType.Splash, TowerType.Slow, TowerType.Sniper,
        TowerType.Chain, TowerType.Pierce, TowerType.Bounce, TowerType.Poison
    };

    public static TowerType RandomType() { return AllTypes[Random.Range(0, AllTypes.Length)]; }

    public static TowerDef Get(TowerType t) { EnsureInit(); return defs[t]; }

    static void EnsureInit()
    {
        if (defs != null) return;
        defs = new Dictionary<TowerType, TowerDef>();

        TowerDef d = new TowerDef();
        d.type = TowerType.SingleShot; d.displayName = "Popcorn Popper"; d.color = new Color(0.95f, 0.80f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 8, range = 4.5f, fireInterval = 0.8f, projectileSpeed = 22f });
        d.tiers.Add(new TowerTierStats { damage = 16, range = 5.5f, fireInterval = 0.65f, projectileSpeed = 24f });
        d.tiers.Add(new TowerTierStats { damage = 30, range = 6.5f, fireInterval = 0.5f, projectileSpeed = 26f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Splash; d.displayName = "Soda Mortar"; d.color = new Color(0.40f, 0.70f, 1f);
        d.tiers.Add(new TowerTierStats { damage = 6, range = 4.0f, fireInterval = 1.2f, projectileSpeed = 16f, splashRadius = 1.6f });
        d.tiers.Add(new TowerTierStats { damage = 12, range = 4.8f, fireInterval = 1.05f, projectileSpeed = 18f, splashRadius = 2.1f });
        d.tiers.Add(new TowerTierStats { damage = 22, range = 5.6f, fireInterval = 0.9f, projectileSpeed = 20f, splashRadius = 2.7f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Slow; d.displayName = "Gum Snare"; d.color = new Color(0.55f, 1f, 0.60f);
        d.tiers.Add(new TowerTierStats { damage = 0, range = 3.5f, fireInterval = 0.5f, slowFactor = 0.45f, slowDuration = 1.2f });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 4.2f, fireInterval = 0.5f, slowFactor = 0.55f, slowDuration = 1.4f });
        d.tiers.Add(new TowerTierStats { damage = 0, range = 5.0f, fireInterval = 0.5f, slowFactor = 0.65f, slowDuration = 1.6f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Sniper; d.displayName = "Pretzel Sniper"; d.color = new Color(0.90f, 0.50f, 0.95f);
        d.tiers.Add(new TowerTierStats { damage = 25, range = 9f, fireInterval = 1.8f });
        d.tiers.Add(new TowerTierStats { damage = 45, range = 11f, fireInterval = 1.6f });
        d.tiers.Add(new TowerTierStats { damage = 80, range = 13f, fireInterval = 1.4f });
        defs[d.type] = d;

        // --- new types ---
        d = new TowerDef();
        d.type = TowerType.Chain; d.displayName = "Sour Static Belt"; d.color = new Color(0.95f, 0.92f, 0.35f);
        d.tiers.Add(new TowerTierStats { damage = 6, range = 4.5f, fireInterval = 1.0f, chainCount = 2, chainRange = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 11, range = 5.2f, fireInterval = 0.9f, chainCount = 3, chainRange = 4.0f });
        d.tiers.Add(new TowerTierStats { damage = 18, range = 6.0f, fireInterval = 0.8f, chainCount = 4, chainRange = 4.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Pierce; d.displayName = "Skewer"; d.color = new Color(0.90f, 0.55f, 0.25f);
        d.tiers.Add(new TowerTierStats { damage = 12, range = 7f, fireInterval = 1.3f, pierceCount = 3, pierceWidth = 1.1f });
        d.tiers.Add(new TowerTierStats { damage = 22, range = 8.5f, fireInterval = 1.2f, pierceCount = 4, pierceWidth = 1.3f });
        d.tiers.Add(new TowerTierStats { damage = 38, range = 10f, fireInterval = 1.1f, pierceCount = 5, pierceWidth = 1.5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Bounce; d.displayName = "Bouncy Ball"; d.color = new Color(0.95f, 0.40f, 0.80f);
        d.tiers.Add(new TowerTierStats { damage = 7, range = 4.5f, fireInterval = 0.9f, projectileSpeed = 18f, bounceCount = 2, bounceRange = 4f });
        d.tiers.Add(new TowerTierStats { damage = 13, range = 5.2f, fireInterval = 0.8f, projectileSpeed = 20f, bounceCount = 3, bounceRange = 4.5f });
        d.tiers.Add(new TowerTierStats { damage = 22, range = 6.0f, fireInterval = 0.7f, projectileSpeed = 22f, bounceCount = 4, bounceRange = 5f });
        defs[d.type] = d;

        d = new TowerDef();
        d.type = TowerType.Poison; d.displayName = "Wasabi"; d.color = new Color(0.50f, 0.90f, 0.30f);
        d.tiers.Add(new TowerTierStats { damage = 2, range = 4.5f, fireInterval = 1.0f, projectileSpeed = 20f, poisonDps = 6f, poisonDuration = 3f });
        d.tiers.Add(new TowerTierStats { damage = 3, range = 5.2f, fireInterval = 0.9f, projectileSpeed = 21f, poisonDps = 11f, poisonDuration = 3.5f });
        d.tiers.Add(new TowerTierStats { damage = 5, range = 6.0f, fireInterval = 0.8f, projectileSpeed = 22f, poisonDps = 18f, poisonDuration = 4f });
        defs[d.type] = d;
    }
}
