using System.Collections.Generic;
using UnityEngine;

// Builds snack-themed visuals for towers and mobs from primitives.
public static class SnackArt
{
    // ------------------------------------------------------------- towers
    public static void BuildTower(Transform root, Transform turret, TowerType type, int tier)
    {
        TowerDef def = TowerCatalog.Get(type);
        Material accent = TDVisuals.Mat(def.color, 0.1f, 0.5f);
        Material light = TDVisuals.Mat(new Color(0.96f, 0.93f, 0.86f), 0f, 0.4f);
        Material dark = TDVisuals.Mat(new Color(0.24f, 0.19f, 0.16f), 0.1f, 0.3f);
        Material red = TDVisuals.Mat(new Color(0.86f, 0.22f, 0.20f), 0f, 0.4f);
        Material silver = TDVisuals.Mat(new Color(0.80f, 0.82f, 0.86f), 0.7f, 0.6f);

        switch (type)
        {
            case TowerType.SingleShot: // popcorn bucket
                TDVisuals.Cyl(root, "Bucket", new Vector3(0f, 0.30f, 0f), 0.40f, 0.60f, light);
                TDVisuals.Cyl(root, "Stripe", new Vector3(0f, 0.46f, 0f), 0.42f, 0.10f, red);
                Popcorn(turret);
                TDVisuals.Box(turret, "Spout", new Vector3(0f, 0.02f, 0.34f), new Vector3(0.14f, 0.14f, 0.34f), red);
                break;

            case TowerType.Splash: // soda can
                TDVisuals.Cyl(root, "Can", new Vector3(0f, 0.35f, 0f), 0.36f, 0.70f, accent);
                TDVisuals.Cyl(root, "Band", new Vector3(0f, 0.50f, 0f), 0.37f, 0.12f, red);
                TDVisuals.Cyl(root, "Lid", new Vector3(0f, 0.72f, 0f), 0.34f, 0.06f, silver);
                GameObject spout = TDVisuals.Cyl(turret, "Spout", new Vector3(0f, 0f, 0.32f), 0.13f, 0.62f, silver);
                spout.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Fizz(turret, accent);
                break;

            case TowerType.Slow: // gumball machine
                TDVisuals.Cyl(root, "Stand", new Vector3(0f, 0.12f, 0f), 0.34f, 0.24f, red);
                TDVisuals.Sphere(root, "Dome", new Vector3(0f, 0.52f, 0f), 0.72f, TDVisuals.Mat(new Color(0.82f, 0.88f, 0.94f), 0.1f, 0.85f));
                Gumballs(root);
                TDVisuals.Sphere(turret, "Cap", new Vector3(0f, 0.05f, 0f), 0.26f, silver);
                break;

            case TowerType.Sniper: // pretzel
                TDVisuals.Cyl(root, "Plate", new Vector3(0f, 0.06f, 0f), 0.42f, 0.12f, light);
                Pretzel(turret);
                GameObject barrel = TDVisuals.Cyl(turret, "Barrel", new Vector3(0f, 0.04f, 0.55f), 0.04f, 0.95f, dark);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject scope = TDVisuals.Cyl(turret, "Scope", new Vector3(0.13f, 0.13f, 0.10f), 0.05f, 0.24f, dark);
                scope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;

            case TowerType.Chain: // sour static belt
                TDVisuals.Cyl(root, "Base", new Vector3(0f, 0.10f, 0f), 0.42f, 0.2f, silver);
                TDVisuals.Box(root, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.16f, 0.5f, 0.16f), dark);
                for (int i = 0; i < 12; i++)
                {
                    float a = i / 12f * Mathf.PI * 2f;
                    TDVisuals.Sphere(turret, "Belt" + i, new Vector3(Mathf.Cos(a) * 0.28f, 0f, Mathf.Sin(a) * 0.28f), 0.14f,
                        TDVisuals.Mat(new Color(0.98f, 0.88f, 0.32f), 0f, 0.6f));
                }
                TDVisuals.Sphere(turret, "Core", new Vector3(0f, 0.05f, 0f), 0.30f, accent);
                break;

            case TowerType.Pierce: // kebab skewer
                TDVisuals.Cyl(root, "Base", new Vector3(0f, 0.10f, 0f), 0.42f, 0.2f, silver);
                TDVisuals.Box(root, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.16f, 0.5f, 0.16f), dark);
                {
                    GameObject sk = TDVisuals.Cyl(turret, "Skewer", new Vector3(0f, 0.05f, 0.5f), 0.03f, 1.2f, silver);
                    sk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    TDVisuals.Box(turret, "Chunk1", new Vector3(0f, 0.05f, 0.25f), new Vector3(0.22f, 0.22f, 0.22f), TDVisuals.Mat(new Color(0.72f, 0.30f, 0.25f), 0f, 0.4f));
                    TDVisuals.Box(turret, "Chunk2", new Vector3(0f, 0.05f, 0.50f), new Vector3(0.20f, 0.20f, 0.20f), TDVisuals.Mat(new Color(0.40f, 0.66f, 0.30f), 0f, 0.4f));
                    TDVisuals.Box(turret, "Chunk3", new Vector3(0f, 0.05f, 0.72f), new Vector3(0.20f, 0.20f, 0.20f), TDVisuals.Mat(new Color(0.85f, 0.62f, 0.30f), 0f, 0.4f));
                }
                break;

            case TowerType.Poison: // wasabi blob
                TDVisuals.Cyl(root, "Base", new Vector3(0f, 0.10f, 0f), 0.42f, 0.2f, silver);
                TDVisuals.Box(root, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.16f, 0.5f, 0.16f), dark);
                TDVisuals.Sphere(turret, "Blob", new Vector3(0f, 0.05f, 0f), 0.50f, accent);
                TDVisuals.Sphere(turret, "Blob2", new Vector3(0.12f, 0.18f, 0.04f), 0.28f, accent);
                TDVisuals.Box(turret, "Leaf", new Vector3(0.15f, 0.28f, 0f), new Vector3(0.24f, 0.03f, 0.12f), TDVisuals.Mat(new Color(0.30f, 0.60f, 0.25f), 0f, 0.4f));
                break;

            case TowerType.Gold: // stack of coins on a post
                TDVisuals.Cyl(root, "Base", new Vector3(0f, 0.10f, 0f), 0.42f, 0.20f, dark);
                TDVisuals.Box(root, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.16f, 0.5f, 0.16f), dark);
                TDVisuals.Cyl(turret, "Coin0", new Vector3(0f, -0.02f, 0f), 0.34f, 0.08f, accent);
                TDVisuals.Cyl(turret, "Coin1", new Vector3(0.03f, 0.06f, 0.02f), 0.32f, 0.08f, accent);
                TDVisuals.Cyl(turret, "Coin2", new Vector3(-0.02f, 0.14f, -0.03f), 0.30f, 0.08f, accent);
                TDVisuals.Cyl(turret, "Coin3", new Vector3(0.02f, 0.22f, 0.02f), 0.28f, 0.08f, accent);
                TDVisuals.Sphere(turret, "Gem", new Vector3(0f, 0.31f, 0f), 0.16f, TDVisuals.Mat(new Color(1f, 0.95f, 0.55f), 0.3f, 0.85f));
                break;
        }

        for (int i = 0; i < tier; i++)
            TDVisuals.Sphere(root, "Pip" + i, new Vector3(-0.16f + i * 0.16f, 0.98f, 0f), 0.09f, TDVisuals.Mat(Color.white, 0f, 0.7f));
    }

    static void Popcorn(Transform t)
    {
        Material pop = TDVisuals.Mat(new Color(1f, 0.95f, 0.72f), 0f, 0.45f);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            float r = 0.10f + (i % 3) * 0.05f;
            TDVisuals.Sphere(t, "Pop" + i, new Vector3(Mathf.Cos(a) * r, 0.06f + (i % 2) * 0.09f, Mathf.Sin(a) * r), 0.22f, pop);
        }
    }

    static void Fizz(Transform t, Material m)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f;
            TDVisuals.Sphere(t, "Fizz" + i, new Vector3(Mathf.Cos(a) * 0.16f, 0.14f + (i % 2) * 0.07f, Mathf.Sin(a) * 0.16f), 0.1f, m);
        }
    }

    static void Gumballs(Transform root)
    {
        Color[] cols =
        {
            new Color(1f, 0.30f, 0.35f), new Color(0.35f, 0.62f, 1f),
            new Color(1f, 0.85f, 0.25f), new Color(0.45f, 1f, 0.45f)
        };
        for (int i = 0; i < 11; i++)
        {
            float a = i / 11f * Mathf.PI * 2f;
            float r = (i % 3) * 0.08f;
            float y = 0.42f + (i % 4) * 0.08f;
            TDVisuals.Sphere(root, "Gum" + i, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), 0.16f, TDVisuals.Mat(cols[i % cols.Length], 0f, 0.6f));
        }
    }

    static void Pretzel(Transform t)
    {
        Material brown = TDVisuals.Mat(new Color(0.72f, 0.46f, 0.18f), 0f, 0.35f);
        int n = 14;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            TDVisuals.Sphere(t, "P" + i, new Vector3(Mathf.Cos(a) * 0.26f, 0.05f, Mathf.Sin(a) * 0.26f), 0.16f, brown);
        }
    }

    // --------------------------------------------------------------- mobs
    public static void BuildMob(Transform root, MobDef def)
    {
        Transform art = new GameObject("Art").transform;
        art.SetParent(root, false);
        art.localScale = Vector3.one * def.scale;

        Material body = TDVisuals.Mat(def.color, 0f, 0.45f);
        Material dark = TDVisuals.Mat(new Color(0.35f, 0.22f, 0.12f), 0f, 0.3f);
        Material icing = TDVisuals.Mat(new Color(1f, 0.55f, 0.75f), 0f, 0.5f);
        Material white = TDVisuals.Mat(new Color(0.98f, 0.95f, 0.90f), 0f, 0.4f);

        switch (def.archetype)
        {
            case MobArchetype.Basic: // cracker
                TDVisuals.Box(art, "Cracker", new Vector3(0f, 0.10f, 0f), new Vector3(0.72f, 0.16f, 0.72f), body);
                for (int i = 0; i < 4; i++)
                    TDVisuals.Sphere(art, "Salt" + i, new Vector3(-0.18f + (i % 2) * 0.36f, 0.20f, -0.18f + (i / 2) * 0.36f), 0.07f, white);
                break;

            case MobArchetype.Fast: // gummy
                TDVisuals.Sphere(art, "Body", new Vector3(0f, 0.30f, 0f), 0.60f, body);
                TDVisuals.Sphere(art, "EarL", new Vector3(-0.18f, 0.52f, 0f), 0.22f, body);
                TDVisuals.Sphere(art, "EarR", new Vector3(0.18f, 0.52f, 0f), 0.22f, body);
                TDVisuals.Sphere(art, "EyeL", new Vector3(-0.12f, 0.34f, 0.24f), 0.08f, white);
                TDVisuals.Sphere(art, "EyeR", new Vector3(0.12f, 0.34f, 0.24f), 0.08f, white);
                break;

            case MobArchetype.Tank: // donut
                TDVisuals.Cyl(art, "Donut", new Vector3(0f, 0.15f, 0f), 0.44f, 0.30f, body);
                TDVisuals.Cyl(art, "Icing", new Vector3(0f, 0.31f, 0f), 0.41f, 0.10f, icing);
                TDVisuals.Cyl(art, "Hole", new Vector3(0f, 0.37f, 0f), 0.12f, 0.08f, dark);
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f;
                    TDVisuals.Box(art, "Spr" + i, new Vector3(Mathf.Cos(a) * 0.26f, 0.38f, Mathf.Sin(a) * 0.26f), new Vector3(0.10f, 0.04f, 0.04f), TDVisuals.Mat(new Color(1f, 0.82f, 0.25f), 0f, 0.6f));
                }
                break;

            case MobArchetype.Swarm: // little berry
                TDVisuals.Sphere(art, "Berry", new Vector3(0f, 0.16f, 0f), 0.34f, body);
                TDVisuals.Cyl(art, "Stem", new Vector3(0f, 0.33f, 0f), 0.035f, 0.10f, dark);
                break;

            case MobArchetype.Boss: // layered cake
                TDVisuals.Cyl(art, "Tier1", new Vector3(0f, 0.15f, 0f), 0.66f, 0.30f, white);
                TDVisuals.Cyl(art, "Tier2", new Vector3(0f, 0.42f, 0f), 0.50f, 0.28f, icing);
                TDVisuals.Cyl(art, "Tier3", new Vector3(0f, 0.68f, 0f), 0.34f, 0.26f, white);
                TDVisuals.Sphere(art, "Cherry", new Vector3(0f, 0.92f, 0f), 0.22f, TDVisuals.Mat(new Color(0.90f, 0.15f, 0.20f), 0f, 0.7f));
                break;
        }
    }

    // --------------------------------------------------------- build ghost
    /// <summary>
    /// Placement preview shown while a build mode is active. Prefers the real 3D
    /// question-mark model at <see cref="SnackModels.GhostPath"/>, falling back to
    /// a procedural base + column + "?" text. The built tower type is random (or
    /// Gold), so no specific tower is previewed. The manager tints every renderer
    /// green/red — with OPAQUE materials, because the player build strips the
    /// alpha-transparent Standard variant.
    /// </summary>
    public static TowerGhost BuildGhost(Transform parent)
    {
        GameObject root = new GameObject("BuildGhost");
        root.transform.SetParent(parent, false);

        TowerGhost ghost = new TowerGhost();
        ghost.validMat = TDVisuals.Mat(new Color(0.35f, 1f, 0.45f), 0f, 0.5f);
        ghost.invalidMat = TDVisuals.Mat(new Color(1f, 0.35f, 0.30f), 0f, 0.5f);

        List<Renderer> parts = new List<Renderer>();

        GameObject prefab = SnackModels.Load(SnackModels.GhostPath);
        if (prefab != null)
        {
            GameObject model = Object.Instantiate(prefab, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // mirror the "?" horizontally

            // normalise to ~1 unit tall, then seat its base on the cell centre
            float h = HeightOf(model);
            if (h > 0.01f) model.transform.localScale *= 1f / h;
            SnackModels.CenterOn(model, root.transform.position);

            parts.AddRange(model.GetComponentsInChildren<Renderer>());
        }
        else
        {
            parts.Add(TDVisuals.Cyl(root.transform, "Base", new Vector3(0f, 0.05f, 0f), 0.55f, 0.10f, ghost.validMat).GetComponent<Renderer>());
            parts.Add(TDVisuals.Cyl(root.transform, "Column", new Vector3(0f, 0.42f, 0f), 0.30f, 0.64f, ghost.validMat).GetComponent<Renderer>());

            GameObject markGO = new GameObject("Mark");
            markGO.transform.SetParent(root.transform, false);
            markGO.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            markGO.AddComponent<BillboardLabel>();   // TextMesh reads from -Z
            TextMesh tm = markGO.AddComponent<TextMesh>();
            tm.text = "?";
            tm.characterSize = 0.12f;
            tm.fontSize = 120;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                tm.font = font;
                MeshRenderer tr = markGO.GetComponent<MeshRenderer>();
                if (tr != null)
                {
                    tr.sharedMaterial = font.material;
                    tr.sortingOrder = 2;
                }
            }
            ghost.mark = tm;
        }

        ghost.parts = parts.ToArray();
        ghost.root = root.transform;
        ghost.SetValid(true);
        return ghost;
    }

    /// <summary>Height (Y extent) of a model's combined renderer bounds.</summary>
    static float HeightOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return 0f;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b.size.y;
    }
}

/// <summary>Root + materials for the opaque "?" build preview.</summary>
public class TowerGhost
{
    public Transform root;
    public Renderer[] parts;
    public TextMesh mark;
    public Material validMat;
    public Material invalidMat;

    /// <summary>Tints the whole preview green (valid + affordable) or red.</summary>
    public void SetValid(bool valid)
    {
        if (root == null) return;
        Material m = valid ? validMat : invalidMat;
        if (parts != null)
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] != null) parts[i].sharedMaterial = m;
        if (mark != null)
            mark.color = valid ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.40f);
    }
}
