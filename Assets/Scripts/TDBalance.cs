using UnityEngine;

/// <summary>Health modifier applied to every mob in a run.</summary>
public enum Difficulty { Easy, Normal, Hard, Insane }

// Central balance. Towers are a FLAT $25.
//
// Because build cost never rises, the limiter has to be income: rewards are
// kept modest so a full run only affords ~15-20 towers. The rest of your
// power must come from MERGING two towers into a stronger tier, which is why
// mob health scales steeply (1.55^wave). Quantity alone won't cut it late.
public static class TDBalance
{
    public const int StartMoney = 100;
    public const int StartLives = 20;
    public const float PrepDuration = 10f;
    public const int TotalWaves = 35;

    public const int BuildCost = 25; // flat, always
    public const int MergeCost = 10; // per merge (2 towers -> 1 of next tier)

    /// <summary>Mob health multiplier for the chosen difficulty.</summary>
    public static float HealthMultiplier(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return 0.75f;
            case Difficulty.Hard: return 1.25f;
            case Difficulty.Insane: return 1.5f;
            default: return 1f;
        }
    }

    public static string DifficultyName(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return "Easy";
            case Difficulty.Hard: return "Hard";
            case Difficulty.Insane: return "Insane";
            default: return "Normal";
        }
    }

    public static string DifficultyBlurb(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return "Mob health -25%";
            case Difficulty.Hard: return "Mob health +25%";
            case Difficulty.Insane: return "Mob health +50%";
            default: return "No modifiers";
        }
    }

    public static Color DifficultyColour(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return new Color(0.45f, 0.9f, 0.5f);
            case Difficulty.Hard: return new Color(1f, 0.72f, 0.3f);
            case Difficulty.Insane: return new Color(1f, 0.34f, 0.32f);
            default: return new Color(0.7f, 0.85f, 1f);
        }
    }

    // Mob health: 1.00, 1.13, 1.28, ...  (~68x by wave 35)
    public static float HealthMult(int wave) { return Mathf.Pow(1.13f, wave - 1); }
    public static float SpeedMult(int wave) { return 1f + 0.02f * (wave - 1); }

    // ------------------------------------------------------------------ waves
    // One mob type per wave, 35 waves. Every 5th is a standalone boss.
    public struct WaveDef
    {
        public string mob;
        public int count;
        public float interval;
        public float startDelay;
        public bool boss;
    }

    static WaveDef W(string mob, int count, float interval, float delay = 0f)
    {
        return new WaveDef { mob = mob, count = count, interval = interval, startDelay = delay };
    }

    static WaveDef B(string mob)
    {
        return new WaveDef { mob = mob, count = 1, interval = 1f, startDelay = 0f, boss = true };
    }

    public static readonly WaveDef[] Waves =
    {
        W("Apple", 8, 0.90f),
        W("Carrot", 10, 0.80f),
        W("Pear", 10, 0.80f),
        W("Banana", 12, 0.60f),
        B("Watermelon"),                       // 5
        W("Cherry", 14, 0.55f),
        W("Potato", 8, 1.20f),
        W("Orange", 14, 0.70f),
        W("Grapes", 18, 0.45f),
        B("Pumpkin"),                          // 10
        W("Corn", 9, 1.30f),
        W("Tomato", 16, 0.65f),
        W("Broccoli", 10, 1.20f),
        W("Strawberry", 20, 0.45f),
        B("Pineapple"),                        // 15
        W("Peach", 18, 0.60f),
        W("Blueberry", 30, 0.30f),
        W("Cucumber", 12, 1.10f),
        W("Plum", 20, 0.55f),
        B("Durian"),                           // 20
        W("Onion", 20, 0.55f),
        W("Radish", 24, 0.45f),
        W("Eggplant", 14, 1.10f),
        W("Kiwi", 22, 0.50f),
        B("Coconut"),                          // 25
        W("Mango", 22, 0.50f),
        W("Raspberry", 34, 0.28f),
        W("Cauliflower", 16, 1.00f),
        W("Beetroot", 18, 0.90f),
        B("Dragonfruit"),                      // 30
        W("Avocado", 18, 1.00f),
        W("Lychee", 38, 0.25f),
        W("Turnip", 20, 1.00f),
        W("Papaya", 22, 0.85f),
        B("GranolaMom"),                       // 35
    };

    // Income: ~$360 over the whole run + $100 start (~18 towers total).
    public static int KillReward(int wave) { return 3; }
    public static int RoundBonus(int wave) { return 15 + 5 * wave; }
}
