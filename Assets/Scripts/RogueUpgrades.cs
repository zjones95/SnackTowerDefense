using System.Collections.Generic;
using UnityEngine;

// Roguelike upgrade pool (issue #8): run-scoped tower bonuses picked after
// boss waves. All effects are universal multipliers/flags read at the existing
// damage/rate/gold hooks, so no TowerCatalog stat is ever mutated.
public enum RogueTier { Common, Rare, Epic }

public struct RogueDef
{
    public string id;
    public string name;
    public string blurb;
    public RogueTier tier;
}

public static class RogueUpgrades
{
    public static readonly RogueDef[] Commons =
    {
        new RogueDef { id = "heavy", name = "Heavy Snacks", blurb = "+10% all tower damage", tier = RogueTier.Common },
        new RogueDef { id = "sugar", name = "Sugar Rush", blurb = "+10% fire rate", tier = RogueTier.Common },
        new RogueDef { id = "straws", name = "Long Straws", blurb = "+12% range", tier = RogueTier.Common },
        new RogueDef { id = "salt", name = "Extra Salt", blurb = "+15% slow strength", tier = RogueTier.Common },
        new RogueDef { id = "sweet", name = "Sweet Tooth", blurb = "+$200 right now", tier = RogueTier.Common },
        new RogueDef { id = "merger", name = "Merger's Market", blurb = "Merge and fuse costs halved", tier = RogueTier.Common },
    };

    public static readonly RogueDef[] Rares =
    {
        new RogueDef { id = "allow", name = "Allowance", blurb = "+$40 every cleared wave", tier = RogueTier.Rare },
        new RogueDef { id = "overclock", name = "Overclock", blurb = "+25% fire rate, -10% range", tier = RogueTier.Rare },
        new RogueDef { id = "lucky", name = "Lucky Dip", blurb = "+8% crit chance (x2) all towers", tier = RogueTier.Rare },
        new RogueDef { id = "caramel", name = "Caramelized", blurb = "Burning enemies take +25%", tier = RogueTier.Rare },
        new RogueDef { id = "happy", name = "Happy Hour", blurb = "Double gold on boss waves", tier = RogueTier.Rare },
        new RogueDef { id = "dessert", name = "Dessert", blurb = "+1% damage per wave cleared", tier = RogueTier.Rare },
    };

    public static readonly RogueDef[] Epics =
    {
        new RogueDef { id = "scoop", name = "Double Scoop", blurb = "+1 projectile on volley towers", tier = RogueTier.Epic },
        new RogueDef { id = "sour", name = "Sour Power", blurb = "Chain jumps deal full damage", tier = RogueTier.Epic },
        new RogueDef { id = "combo", name = "Combo Meal", blurb = "+2% damage per distinct type+tier alive", tier = RogueTier.Epic },
        new RogueDef { id = "artillery", name = "Artillery", blurb = "Snipers +30% damage, rods pierce +1", tier = RogueTier.Epic },
        new RogueDef { id = "overdrive", name = "Overdrive", blurb = "+30% fire rate on boss waves", tier = RogueTier.Epic },
        new RogueDef { id = "slayer", name = "Giant Slayer", blurb = "Top-tier towers deal +2% current HP per hit", tier = RogueTier.Epic },
    };

    /// <summary>Draws <paramref name="count"/> distinct random options from a pool.</summary>
    public static List<RogueDef> Offer(RogueDef[] pool, int count)
    {
        List<RogueDef> opts = new List<RogueDef>(pool);
        // partial Fisher-Yates: shuffle only as many as needed
        for (int i = 0; i < Mathf.Min(count, opts.Count); i++)
        {
            int j = Random.Range(i, opts.Count);
            RogueDef tmp = opts[i]; opts[i] = opts[j]; opts[j] = tmp;
        }
        return opts.GetRange(0, Mathf.Min(count, opts.Count));
    }

    public static Color TierColor(RogueTier t)
    {
        switch (t)
        {
            case RogueTier.Rare: return new Color(0.55f, 0.55f, 1f);    // blue-violet
            case RogueTier.Epic: return new Color(1f, 0.85f, 0.35f);    // gold
            default: return new Color(0.62f, 0.68f, 0.76f);              // grey-blue
        }
    }
}

/// <summary>Live run-scoped modifiers for the local board. Multipliers stack
/// multiplicatively across repeat picks. Reset on every new run; the Damage
/// Test intentionally keeps them (it reuses the victory board).</summary>
public static class RogueMods
{
    public static float Damage = 1f;
    public static float Rate = 1f;
    public static float Range = 1f;
    public static float SlowStrength = 1f;
    public static float CritChance = 0f;
    public const float CritMult = 2f;
    public static float MergeMult = 1f;
    public static int Wage = 0;
    public static int WavesCleared = 0;

    public static bool Caramelized;
    public static bool HappyHour;
    public static bool DoubleScoop;
    public static bool SourPower;
    public static bool ComboMeal;
    public static bool Artillery;
    public static bool Overdrive;
    public static bool GiantSlayer;

    public static readonly List<string> Owned = new List<string>();

    // cached census of the live towers (refreshed by the manager)
    public static float ComboMult = 1f;
    public static int MaxTier;

    public static void Reset()
    {
        Damage = Rate = Range = SlowStrength = 1f;
        CritChance = 0f;
        MergeMult = 1f;
        Wage = 0;
        WavesCleared = 0;
        Caramelized = HappyHour = DoubleScoop = SourPower = false;
        ComboMeal = Artillery = Overdrive = GiantSlayer = false;
        Owned.Clear();
        ComboMult = 1f;
        MaxTier = 0;
    }

    /// <summary>All tower damage scales with the local run's roguelike mods.</summary>
    public static float DamageMult()
    {
        return Damage * (1f + 0.01f * WavesCleared) * ComboMult;
    }

    /// <summary>Effective fire-rate multiplier (Overdrive only on boss waves).</summary>
    public static float EffRate(bool bossWave)
    {
        return Rate * (Overdrive && bossWave ? 1.3f : 1f);
    }

    /// <summary>Effective range multiplier (Overclock trims range).</summary>
    public static float EffRange(float baseRange)
    {
        return baseRange * Range;
    }

    /// <summary>Effective gold payout (Happy Hour doubles on boss waves).</summary>
    public static int EffGold(int baseGold, bool bossWave)
    {
        return HappyHour && bossWave ? baseGold * 2 : baseGold;
    }

    /// <summary>Effective merge/fuse cost.</summary>
    public static int EffMergeCost(int baseCost)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseCost * MergeMult));
    }

    public static bool IsBossWave(int wave)
    {
        int i = Mathf.Clamp(wave - 1, 0, TDBalance.Waves.Length - 1);
        return TDBalance.Waves[i].boss;
    }
}
