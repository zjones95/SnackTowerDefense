using System;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod LateWaveCheck.Verify -quit
public static class LateWaveCheck
{
    public static void Verify()
    {
        if (TDBalance.TotalWaves != 50 || TDBalance.Waves.Length != TDBalance.TotalWaves)
            throw new Exception("Wave count does not match TotalWaves");

        string[] expected = { "CaesarSalad", "Starfruit", "PomegranateSeed",
                              "Artichoke", "Asparagus", "GranolaMom" };
        int[] counts = { 1, 26, 40, 16, 22, 1 };
        for (int i = 0; i < expected.Length; i++)
        {
            int wave = 45 + i;
            TDBalance.WaveDef w = TDBalance.Waves[wave - 1];
            MobDef def = MobCatalog.Get(expected[i]);
            if (w.mob != expected[i] || w.count != counts[i] || def.id != expected[i]
                || w.boss != (wave % 5 == 0))
                throw new Exception("Invalid wave " + wave + ": " + w.mob);
            if (SnackModels.Load(MobCatalog.ModelPath(def)) == null)
                throw new Exception("Missing model for wave " + wave);
        }

        for (int wave = 5; wave <= 50; wave += 5)
        {
            TDBalance.WaveDef w = TDBalance.Waves[wave - 1];
            if (!w.boss || w.count != 1 || MobCatalog.Get(w.mob).archetype != MobArchetype.Boss)
                throw new Exception("Missing standalone boss at wave " + wave);
        }

        // Waves 31+ carry an intentional extra +25% HP late-game ramp (see
        // TDBalance.HealthMult). Waves 1-30 are unchanged; these values would
        // only move if someone edits the curve again.
        if (Mathf.Abs(TDBalance.HealthMult(45) - 36.908f) > 0.05f)
            throw new Exception("Wave 45 health curve changed");
        float caesar = MobCatalog.Get("CaesarSalad").health * TDBalance.HealthMult(45) * TDBalance.MobHealthScale;
        float granola = MobCatalog.Get("GranolaMom").health * TDBalance.HealthMult(50) * TDBalance.MobHealthScale;
        if (caesar < 56000f || caesar > 60500f || granola < 71500f || granola > 77000f)
            throw new Exception("Boss HP outside intended range: " + caesar + " / " + granola);
        Debug.Log("LateWaveCheck OK: 50 waves, Caesar " + caesar.ToString("F0")
                  + " HP, Granola Mom " + granola.ToString("F0") + " HP on Normal");
        MobWalkCheck.VerifyLateWaveArt();
    }
}
