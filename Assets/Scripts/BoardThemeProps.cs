using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full-environment dressing per board theme: a ground slab under the grid plus
/// themed props on void cells and (full-size boards only) a perimeter ring.
/// Everything is built from primitives sized off map.Cell, so the same code
/// dresses the in-game board and the miniature setup-screen preview. Classic
/// builds nothing here — the kid's room (TDRoom) is its surroundings.
/// </summary>
public static class BoardThemeProps
{
    static readonly Dictionary<string, Material> glowCache = new Dictionary<string, Material>();

    static Material Glow(Color c, float emit)
    {
        string key = c.r + "_" + c.g + "_" + c.b + "_" + emit;
        Material m;
        if (glowCache.TryGetValue(key, out m) && m != null) return m;
        m = BoardThemes.GlowMat(c, emit);
        glowCache[key] = m;
        return m;
    }

    public static void Build(Transform parent, TDMap map, BoardTheme theme, bool perimeter)
    {
        if (theme == BoardTheme.Classic) return;

        // Deterministic layout per theme; restore the global random state after.
        Random.State old = Random.state;
        Random.InitState((int)theme * 1000 + 7);

        float c = map.Cell;
        float W = map.Width * c, H = map.Height * c;
        BoardThemeDef th = BoardThemes.Get(theme);

        // Ground slab: darker sibling of the void colour, top just under the tiles.
        Color g = th.voidC * 0.55f;
        TDVisuals.Box(parent, "Ground", new Vector3(0f, -0.055f, 0f),
            new Vector3(W + 4f * c, 0.10f, H + 4f * c), TDVisuals.Mat(g, 0f, 0.6f));

        // Props on void cells only (never on path / buildable / start / end).
        for (int ly = 0; ly < map.Height; ly++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.At(x, ly) != 'x') continue;
                if (Random.Range(0f, 1f) > 0.55f) continue;
                Vector3 p = map.CellCenter(x, ly);
                p.y = 0f;
                BuildProp(parent, theme, p, c, 1f);
            }

        // Perimeter ring outside the board (skipped in the setup preview so it
        // keeps framing; void-cell props already show there).
        if (perimeter)
        {
            for (int k = 0; k < 10; k++)
            {
                float a = k / 10f * Mathf.PI * 2f + 0.3f;
                float r = Mathf.Max(W, H) * 0.5f + c * (1.2f + (k % 3) * 0.7f);
                Vector3 p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r) + map.Origin + new Vector3(W * 0.5f, 0f, H * 0.5f);
                BuildProp(parent, theme, p, c, 1.5f);
            }
        }

        Random.state = old;
    }

    static void BuildProp(Transform parent, BoardTheme theme, Vector3 p, float c, float mul)
    {
        switch (theme)
        {
            case BoardTheme.Emberfall:
                if (Random.Range(0, 3) == 0) LavaPool(parent, p, c);
                else if (Random.Range(0, 2) == 0) BasaltSpike(parent, p, c, mul);
                else Rock(parent, p, c, mul, new Color(0.12f, 0.10f, 0.10f));
                break;
            case BoardTheme.Glacier:
                if (Random.Range(0, 3) == 0) SnowPile(parent, p, c);
                else if (Random.Range(0, 2) == 0) IceShard(parent, p, c, mul);
                else SnowPine(parent, p, c, mul);
                break;
            case BoardTheme.Mosswood:
                if (Random.Range(0, 3) == 0) Mushroom(parent, p, c);
                else if (Random.Range(0, 2) == 0) Tree(parent, p, c, mul);
                else Rock(parent, p, c, mul, new Color(0.45f, 0.45f, 0.47f));
                break;
            case BoardTheme.DuneSea:
                if (Random.Range(0, 3) == 0) Dune(parent, p, c);
                else if (Random.Range(0, 2) == 0) Cactus(parent, p, c, mul);
                else SandPillar(parent, p, c, mul);
                break;
            case BoardTheme.MidnightCircuit:
                if (Random.Range(0, 2) == 0) Pylon(parent, p, c, mul);
                else GlowOrb(parent, p, c, mul);
                break;
            case BoardTheme.Chocolatier:
                if (Random.Range(0, 4) == 0) CandyCane(parent, p, c);
                else if (Random.Range(0, 3) == 0) Cupcake(parent, p, c);
                else if (Random.Range(0, 2) == 0) ChocBar(parent, p, c);
                else Lollipop(parent, p, c);
                break;
            case BoardTheme.Blueprint:
                if (Random.Range(0, 3) == 0) Pencil(parent, p, c);
                else if (Random.Range(0, 2) == 0) Ruler(parent, p, c);
                else PaperStack(parent, p, c);
                break;
        }
    }

    // ------------------------------------------------------------- primitives
    static GameObject Part(Transform parent, string name, Vector3 pos, Vector3 scale, Material m, float rotY = 0f)
    {
        GameObject g = TDVisuals.Box(parent, name, pos, scale, m);
        if (Mathf.Abs(rotY) > 0.01f) g.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        return g;
    }

    static void Rock(Transform parent, Vector3 p, float c, float mul, Color col)
    {
        Material m = TDVisuals.Mat(col, 0f, 0.8f);
        float r = Random.Range(0f, 180f);
        Part(parent, "Rock", p + new Vector3(0f, 0.16f * c * mul, 0f),
            new Vector3(0.50f * c * mul, 0.32f * c * mul, 0.42f * c * mul), m, r);
        Part(parent, "RockTop", p + new Vector3(0.08f * c, 0.34f * c * mul, -0.05f * c),
            new Vector3(0.26f * c * mul, 0.18f * c * mul, 0.22f * c * mul), m, r + 40f);
    }

    static void LavaPool(Transform parent, Vector3 p, float c)
    {
        GameObject g = TDVisuals.Cyl(parent, "Lava", p + new Vector3(0f, 0.02f, 0f),
            0.38f * c, 0.04f, Glow(new Color(1f, 0.42f, 0.08f), 1.4f));
        g.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
    }

    static void BasaltSpike(Transform parent, Vector3 p, float c, float mul)
    {
        Part(parent, "Spike", p + new Vector3(0f, 0.45f * c * mul, 0f),
            new Vector3(0.15f * c, 0.90f * c * mul, 0.15f * c),
            TDVisuals.Mat(new Color(0.10f, 0.09f, 0.10f), 0f, 0.8f), Random.Range(0f, 180f));
    }

    static void SnowPile(Transform parent, Vector3 p, float c)
    {
        GameObject g = TDVisuals.Sphere(parent, "Snow", p + new Vector3(0f, 0.10f * c, 0f),
            0.70f * c, TDVisuals.Mat(new Color(0.94f, 0.96f, 1f), 0f, 0.4f));
        g.transform.localScale = new Vector3(0.70f * c, 0.28f * c, 0.70f * c);
    }

    static void IceShard(Transform parent, Vector3 p, float c, float mul)
    {
        GameObject g = Part(parent, "Shard", p + new Vector3(0f, 0.40f * c * mul, 0f),
            new Vector3(0.16f * c, 0.80f * c * mul, 0.16f * c),
            TDVisuals.Mat(new Color(0.65f, 0.87f, 1f), 0.1f, 0.15f), Random.Range(0f, 180f));
        g.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), g.transform.rotation.eulerAngles.y, Random.Range(-12f, 12f));
    }

    static void SnowPine(Transform parent, Vector3 p, float c, float mul)
    {
        TDVisuals.Cyl(parent, "Trunk", p + new Vector3(0f, 0.15f * c * mul, 0f),
            0.08f * c, 0.30f * c * mul, TDVisuals.Mat(new Color(0.35f, 0.25f, 0.15f), 0f, 0.8f));
        Material leaf = TDVisuals.Mat(new Color(0.55f, 0.75f, 0.62f), 0f, 0.6f);
        GameObject g1 = TDVisuals.Sphere(parent, "Pine1", p + new Vector3(0f, 0.45f * c * mul, 0f), 0.55f * c * mul, leaf);
        GameObject g2 = TDVisuals.Sphere(parent, "Pine2", p + new Vector3(0f, 0.72f * c * mul, 0f), 0.38f * c * mul, leaf);
        g1.transform.localScale *= 0.9f; g2.transform.localScale *= 0.9f;
    }

    static void Tree(Transform parent, Vector3 p, float c, float mul)
    {
        TDVisuals.Cyl(parent, "Trunk", p + new Vector3(0f, 0.20f * c * mul, 0f),
            0.10f * c, 0.40f * c * mul, TDVisuals.Mat(new Color(0.40f, 0.28f, 0.15f), 0f, 0.8f));
        TDVisuals.Sphere(parent, "Canopy", p + new Vector3(0f, 0.62f * c * mul, 0f),
            0.72f * c * mul, TDVisuals.Mat(new Color(0.22f, 0.48f, 0.22f), 0f, 0.7f));
    }

    static void Mushroom(Transform parent, Vector3 p, float c)
    {
        TDVisuals.Cyl(parent, "Stem", p + new Vector3(0f, 0.12f * c, 0f),
            0.09f * c, 0.24f * c, TDVisuals.Mat(new Color(0.92f, 0.88f, 0.78f), 0f, 0.6f));
        GameObject cap = TDVisuals.Sphere(parent, "Cap", p + new Vector3(0f, 0.26f * c, 0f),
            0.34f * c, TDVisuals.Mat(new Color(0.80f, 0.20f, 0.20f), 0f, 0.5f));
        cap.transform.localScale = new Vector3(0.34f * c, 0.20f * c, 0.34f * c);
    }

    static void Cactus(Transform parent, Vector3 p, float c, float mul)
    {
        Material m = TDVisuals.Mat(new Color(0.25f, 0.55f, 0.28f), 0f, 0.6f);
        TDVisuals.Cyl(parent, "Cactus", p + new Vector3(0f, 0.35f * c * mul, 0f), 0.12f * c, 0.70f * c * mul, m);
        GameObject arm = TDVisuals.Cyl(parent, "Arm", p + new Vector3(0.16f * c, 0.38f * c * mul, 0f), 0.08f * c, 0.30f * c, m);
        arm.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        TDVisuals.Cyl(parent, "ArmUp", p + new Vector3(0.28f * c, 0.48f * c * mul, 0f), 0.08f * c, 0.24f * c, m);
    }

    static void Dune(Transform parent, Vector3 p, float c)
    {
        GameObject g = TDVisuals.Sphere(parent, "Dune", p + new Vector3(0f, 0.06f * c, 0f),
            0.80f * c, TDVisuals.Mat(new Color(0.85f, 0.72f, 0.52f), 0f, 0.7f));
        g.transform.localScale = new Vector3(0.80f * c, 0.22f * c, 0.80f * c);
    }

    static void SandPillar(Transform parent, Vector3 p, float c, float mul)
    {
        Part(parent, "Pillar", p + new Vector3(0f, 0.40f * c * mul, 0f),
            new Vector3(0.28f * c, 0.80f * c * mul, 0.28f * c),
            TDVisuals.Mat(new Color(0.78f, 0.64f, 0.45f), 0f, 0.7f), Random.Range(0f, 180f));
        Part(parent, "PillarCap", p + new Vector3(0f, 0.83f * c * mul, 0f),
            new Vector3(0.36f * c, 0.10f * c, 0.36f * c),
            TDVisuals.Mat(new Color(0.70f, 0.55f, 0.38f), 0f, 0.7f), Random.Range(0f, 180f));
    }

    static void Pylon(Transform parent, Vector3 p, float c, float mul)
    {
        Part(parent, "Pylon", p + new Vector3(0f, 0.50f * c * mul, 0f),
            new Vector3(0.18f * c, 1.00f * c * mul, 0.18f * c),
            TDVisuals.Mat(new Color(0.05f, 0.06f, 0.10f), 0.2f, 0.5f), Random.Range(0f, 180f));
        Part(parent, "PylonTip", p + new Vector3(0f, 1.03f * c * mul, 0f),
            new Vector3(0.24f * c, 0.10f * c, 0.24f * c), Glow(new Color(0.10f, 0.85f, 1f), 1.6f));
    }

    static void GlowOrb(Transform parent, Vector3 p, float c, float mul)
    {
        TDVisuals.Cyl(parent, "Pole", p + new Vector3(0f, 0.30f * c * mul, 0f),
            0.05f * c, 0.60f * c * mul, TDVisuals.Mat(new Color(0.05f, 0.06f, 0.10f), 0.2f, 0.5f));
        TDVisuals.Sphere(parent, "Orb", p + new Vector3(0f, 0.66f * c * mul, 0f),
            0.22f * c, Glow(new Color(1f, 0.25f, 0.85f), 1.6f));
    }

    static void CandyCane(Transform parent, Vector3 p, float c)
    {
        Material red = TDVisuals.Mat(new Color(0.85f, 0.15f, 0.20f), 0f, 0.4f);
        Material white = TDVisuals.Mat(new Color(0.96f, 0.95f, 0.93f), 0f, 0.4f);
        for (int i = 0; i < 5; i++)
            TDVisuals.Cyl(parent, "Cane", p + new Vector3(0f, 0.07f * c + i * 0.13f * c, 0f),
                0.09f * c, 0.13f * c, i % 2 == 0 ? red : white);
    }

    static void Cupcake(Transform parent, Vector3 p, float c)
    {
        TDVisuals.Cyl(parent, "Cup", p + new Vector3(0f, 0.09f * c, 0f),
            0.16f * c, 0.18f * c, TDVisuals.Mat(new Color(0.55f, 0.35f, 0.20f), 0f, 0.7f));
        GameObject top = TDVisuals.Sphere(parent, "Frost", p + new Vector3(0f, 0.24f * c, 0f),
            0.26f * c, TDVisuals.Mat(new Color(0.96f, 0.65f, 0.78f), 0f, 0.5f));
        top.transform.localScale = new Vector3(0.26f * c, 0.18f * c, 0.26f * c);
    }

    static void ChocBar(Transform parent, Vector3 p, float c)
    {
        Part(parent, "Choc", p + new Vector3(0f, 0.05f * c, 0f),
            new Vector3(0.42f * c, 0.10f * c, 0.26f * c),
            TDVisuals.Mat(new Color(0.35f, 0.20f, 0.10f), 0f, 0.5f), Random.Range(0f, 180f));
    }

    static void Lollipop(Transform parent, Vector3 p, float c)
    {
        TDVisuals.Cyl(parent, "Stick", p + new Vector3(0f, 0.20f * c, 0f),
            0.035f * c, 0.40f * c, TDVisuals.Mat(new Color(0.95f, 0.93f, 0.88f), 0f, 0.5f));
        Color[] cols = { new Color(0.95f, 0.25f, 0.35f), new Color(0.25f, 0.65f, 0.95f), new Color(0.35f, 0.80f, 0.35f) };
        TDVisuals.Sphere(parent, "Pop", p + new Vector3(0f, 0.48f * c, 0f),
            0.24f * c, TDVisuals.Mat(cols[Random.Range(0, 3)], 0f, 0.3f));
    }

    static void Pencil(Transform parent, Vector3 p, float c)
    {
        float r = Random.Range(0f, 180f);
        Quaternion q = Quaternion.Euler(0f, r, 90f);
        GameObject body = TDVisuals.Cyl(parent, "Pencil", p + new Vector3(0f, 0.07f * c, 0f),
            0.06f * c, 0.80f * c, TDVisuals.Mat(new Color(0.95f, 0.75f, 0.15f), 0f, 0.5f));
        body.transform.rotation = q;
        GameObject tip = TDVisuals.Cyl(parent, "Tip", p + new Vector3(Mathf.Cos(r * Mathf.Deg2Rad) * 0.45f * c, 0.07f * c, -Mathf.Sin(r * Mathf.Deg2Rad) * 0.45f * c),
            0.06f * c, 0.14f * c, TDVisuals.Mat(new Color(0.92f, 0.82f, 0.68f), 0f, 0.6f));
        tip.transform.rotation = q;
    }

    static void Ruler(Transform parent, Vector3 p, float c)
    {
        Part(parent, "Ruler", p + new Vector3(0f, 0.03f * c, 0f),
            new Vector3(0.75f * c, 0.04f * c, 0.16f * c),
            TDVisuals.Mat(new Color(0.93f, 0.90f, 0.78f), 0f, 0.5f), Random.Range(0f, 180f));
    }

    static void PaperStack(Transform parent, Vector3 p, float c)
    {
        Material m = TDVisuals.Mat(new Color(0.94f, 0.95f, 0.97f), 0f, 0.5f);
        Part(parent, "Paper1", p + new Vector3(0f, 0.02f * c, 0f),
            new Vector3(0.55f * c, 0.035f * c, 0.70f * c), m, Random.Range(0f, 180f));
        Part(parent, "Paper2", p + new Vector3(0f, 0.055f * c, 0f),
            new Vector3(0.55f * c, 0.035f * c, 0.70f * c), m, Random.Range(0f, 180f));
    }
}
