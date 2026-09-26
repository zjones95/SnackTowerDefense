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
    public const int TotalWaves = 5;

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

    // Mob health: 1.00, 1.55, 2.40, 3.72, 5.77
    public static float HealthMult(int wave) { return Mathf.Pow(1.55f, wave - 1); }
    public static float SpeedMult(int wave) { return 1f + 0.04f * (wave - 1); }

    // Income: ~$360 over the whole run + $100 start (~18 towers total).
    public static int KillReward(int wave) { return 3; }
    public static int RoundBonus(int wave) { return 15 + 5 * wave; }
}
