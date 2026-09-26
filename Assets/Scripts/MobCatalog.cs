using System.Collections.Generic;
using UnityEngine;

/// <summary>Behaviour family for a mob. Every wave uses exactly one mob.</summary>
public enum MobArchetype { Basic, Fast, Tank, Swarm, Boss }

[System.Serializable]
public class MobDef
{
    public string id;            // also the model file: Snack/Mobs/<id>
    public string displayName;
    public MobArchetype archetype;
    public int index;            // position in MobCatalog.All (sent over the wire)

    public float health = 30f;
    public float speed = 1.7f;
    public int leakDamage = 1;
    public float scale = 0.7f;
    public Color color = Color.red;

    // ---- boss-only behaviours ----
    public float regen = 0f;         // health restored per second
    public float armour = 0f;        // flat damage removed from every hit
    public bool slowImmune = false;
    public float enrage = 0f;        // extra speed fraction at zero health
    public float dashEvery = 0f;     // seconds between dashes (0 = never)
    public bool longHair = false;    // procedural humanoid: long hair
}

// The 35-wave roster. Waves live in TDBalance; this only describes the mobs.
public static class MobCatalog
{
    public static readonly MobDef[] All =
    {
        // wave 1-4
        Make("Apple", MobArchetype.Basic),
        Make("Carrot", MobArchetype.Basic),
        Make("Pear", MobArchetype.Basic),
        Make("Banana", MobArchetype.Fast),
        Make("Watermelon", MobArchetype.Boss),        // 5
        Make("Cherry", MobArchetype.Fast),
        Make("Potato", MobArchetype.Tank),
        Make("Orange", MobArchetype.Basic),
        Make("Grapes", MobArchetype.Fast),
        Make("Pumpkin", MobArchetype.Boss),           // 10
        Make("Corn", MobArchetype.Tank),
        Make("Tomato", MobArchetype.Basic),
        Make("Broccoli", MobArchetype.Tank),
        Make("Strawberry", MobArchetype.Fast),
        Make("Pineapple", MobArchetype.Boss),         // 15
        Make("Peach", MobArchetype.Basic),
        Make("Blueberry", MobArchetype.Swarm),
        Make("Cucumber", MobArchetype.Tank),
        Make("Plum", MobArchetype.Basic),
        Make("Durian", MobArchetype.Boss),            // 20
        Make("Onion", MobArchetype.Basic),
        Make("Radish", MobArchetype.Fast),
        Make("Eggplant", MobArchetype.Tank),
        Make("Kiwi", MobArchetype.Basic),
        Make("Coconut", MobArchetype.Boss),           // 25
        Make("Mango", MobArchetype.Basic),
        Make("Raspberry", MobArchetype.Swarm),
        Make("Cauliflower", MobArchetype.Tank),
        Make("Beetroot", MobArchetype.Tank),
        Make("Dragonfruit", MobArchetype.Boss),       // 30
        Make("Avocado", MobArchetype.Tank),
        Make("Lychee", MobArchetype.Swarm),
        Make("Turnip", MobArchetype.Tank),
        Make("Papaya", MobArchetype.Tank),
        Make("GranolaMom", MobArchetype.Boss),        // 35 — final boss
    };

    private static Dictionary<string, MobDef> byId;

    static MobCatalog()
    {
        for (int i = 0; i < All.Length; i++) All[i].index = i;
    }

    public static MobDef Get(string id)
    {
        if (byId == null)
        {
            byId = new Dictionary<string, MobDef>();
            for (int i = 0; i < All.Length; i++) byId[All[i].id] = All[i];
        }
        MobDef d;
        return byId.TryGetValue(id, out d) ? d : All[0];
    }

    public static string ModelPath(MobDef def) { return "Snack/Mobs/" + def.id; }

    static MobDef Make(string id, MobArchetype a)
    {
        MobDef d = new MobDef { id = id, displayName = Name(id), archetype = a };

        switch (a)
        {
            case MobArchetype.Fast: d.health = 20f; d.speed = 3.0f; d.scale = 0.55f; break;
            case MobArchetype.Tank: d.health = 95f; d.speed = 1.15f; d.leakDamage = 2; d.scale = 0.95f; break;
            case MobArchetype.Swarm: d.health = 12f; d.speed = 3.4f; d.scale = 0.42f; break;
            case MobArchetype.Boss: d.health = 420f; d.speed = 0.95f; d.leakDamage = 5; d.scale = 1.45f; break;
            default: d.health = 30f; d.speed = 1.7f; d.scale = 0.7f; break;
        }

        d.color = Colour(id);

        // ---- boss personalities ----
        switch (id)
        {
            case "Watermelon": d.health = 520f; d.speed = 0.72f; break;
            case "Pumpkin": d.health = 500f; d.regen = 9f; break;
            case "Pineapple": d.health = 520f; d.armour = 4f; break;
            case "Durian": d.health = 540f; d.enrage = 0.85f; break;
            case "Coconut": d.health = 700f; d.speed = 0.68f; d.slowImmune = true; break;
            case "Dragonfruit": d.health = 620f; d.dashEvery = 4.5f; break;
            case "GranolaMom": d.health = 820f; d.slowImmune = true; d.enrage = 0.6f; d.dashEvery = 6f; d.longHair = true; break;
        }
        return d;
    }

    static string Name(string id)
    {
        if (id == "GranolaMom") return "Granola Mom";
        return id;
    }

    static Color Colour(string id)
    {
        switch (id)
        {
            case "Apple": return new Color(0.85f, 0.16f, 0.18f);
            case "Carrot": return new Color(0.93f, 0.51f, 0.13f);
            case "Pear": return new Color(0.72f, 0.82f, 0.28f);
            case "Banana": return new Color(0.96f, 0.86f, 0.28f);
            case "Watermelon": return new Color(0.30f, 0.66f, 0.30f);
            case "Cherry": return new Color(0.78f, 0.10f, 0.20f);
            case "Potato": return new Color(0.72f, 0.56f, 0.34f);
            case "Orange": return new Color(0.96f, 0.58f, 0.12f);
            case "Grapes": return new Color(0.55f, 0.28f, 0.72f);
            case "Pumpkin": return new Color(0.92f, 0.48f, 0.10f);
            case "Corn": return new Color(0.96f, 0.84f, 0.28f);
            case "Tomato": return new Color(0.88f, 0.18f, 0.14f);
            case "Broccoli": return new Color(0.24f, 0.54f, 0.22f);
            case "Strawberry": return new Color(0.90f, 0.20f, 0.28f);
            case "Pineapple": return new Color(0.92f, 0.78f, 0.22f);
            case "Peach": return new Color(0.96f, 0.60f, 0.44f);
            case "Blueberry": return new Color(0.30f, 0.36f, 0.76f);
            case "Cucumber": return new Color(0.36f, 0.66f, 0.26f);
            case "Plum": return new Color(0.52f, 0.24f, 0.52f);
            case "Durian": return new Color(0.66f, 0.62f, 0.24f);
            case "Onion": return new Color(0.86f, 0.74f, 0.62f);
            case "Radish": return new Color(0.88f, 0.24f, 0.34f);
            case "Eggplant": return new Color(0.40f, 0.22f, 0.52f);
            case "Kiwi": return new Color(0.56f, 0.68f, 0.28f);
            case "Coconut": return new Color(0.46f, 0.32f, 0.20f);
            case "Mango": return new Color(0.94f, 0.56f, 0.16f);
            case "Raspberry": return new Color(0.80f, 0.16f, 0.34f);
            case "Cauliflower": return new Color(0.90f, 0.88f, 0.76f);
            case "Beetroot": return new Color(0.62f, 0.14f, 0.34f);
            case "Dragonfruit": return new Color(0.90f, 0.24f, 0.48f);
            case "Avocado": return new Color(0.44f, 0.56f, 0.24f);
            case "Lychee": return new Color(0.86f, 0.36f, 0.36f);
            case "Turnip": return new Color(0.84f, 0.80f, 0.72f);
            case "Papaya": return new Color(0.92f, 0.62f, 0.26f);
            case "GranolaMom": return new Color(0.72f, 0.58f, 0.42f);
            default: return new Color(0.8f, 0.8f, 0.8f);
        }
    }
}
