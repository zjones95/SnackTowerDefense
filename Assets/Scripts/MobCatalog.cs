using System.Collections.Generic;
using UnityEngine;

public enum MobType { Basic, Fast, Tank, Boss }

[System.Serializable]
public class MobDef
{
    public MobType type;
    public string displayName;
    public float health = 30f;
    public float speed = 1.6f;
    public int leakDamage = 1;
    public float scale = 0.7f;
    public Color color = Color.red;
}

public static class MobCatalog
{
    private static Dictionary<MobType, MobDef> defs;

    public static MobDef Get(MobType t)
    {
        EnsureInit();
        return defs[t];
    }

    static void EnsureInit()
    {
        if (defs != null) return;
        defs = new Dictionary<MobType, MobDef>();

        MobDef d = new MobDef { type = MobType.Basic, displayName = "Cracker", health = 30f, speed = 1.7f, leakDamage = 1, scale = 0.7f, color = new Color(0.92f, 0.78f, 0.42f) };
        defs[d.type] = d;

        d = new MobDef { type = MobType.Fast, displayName = "Gummy", health = 20f, speed = 3.1f, leakDamage = 1, scale = 0.55f, color = new Color(1f, 0.40f, 0.52f) };
        defs[d.type] = d;

        d = new MobDef { type = MobType.Tank, displayName = "Donut", health = 95f, speed = 1.15f, leakDamage = 2, scale = 0.95f, color = new Color(0.72f, 0.46f, 0.24f) };
        defs[d.type] = d;

        d = new MobDef { type = MobType.Boss, displayName = "Cake Boss", health = 340f, speed = 0.95f, leakDamage = 5, scale = 1.5f, color = new Color(0.95f, 0.55f, 0.88f) };
        defs[d.type] = d;
    }
}
