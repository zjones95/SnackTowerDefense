using UnityEngine;

// Board skins: the grid, route and rules never change — only the visuals.
// Single player picks on the difficulty screen (persisted in PlayerPrefs);
// multiplayer carries each player's choice in the lobby roster so remote
// boards render in their owner's theme.
public enum BoardTheme
{
    Classic = 0,
    Emberfall = 1,
    Glacier = 2,
    Mosswood = 3,
    DuneSea = 4,
    MidnightCircuit = 5,
    Chocolatier = 6,
    Blueprint = 7,
}

public struct BoardThemeDef
{
    public string displayName;
    public Color[] tiles;     // 4-way checker for buildable cells
    public Color path;        // mob route
    public Color voidC;       // neutral filler
    public Color start;       // spawn cell
    public Color end;         // end cell
    public bool flatPath;     // skip the Road texture (clean vector look)
    public bool glowPath;     // emissive route/start/end (MidnightCircuit)
    public Color swatch;      // UI picker colour
}

public static class BoardThemes
{
    public const string PrefsKey = "td_board_theme";
    public static readonly BoardTheme[] All =
    {
        BoardTheme.Classic, BoardTheme.Emberfall, BoardTheme.Glacier, BoardTheme.Mosswood,
        BoardTheme.DuneSea, BoardTheme.MidnightCircuit, BoardTheme.Chocolatier, BoardTheme.Blueprint,
    };

    public static BoardTheme Load()
    {
        int v = PlayerPrefs.GetInt(PrefsKey, 0);
        return v >= 0 && v < All.Length ? (BoardTheme)v : BoardTheme.Classic;
    }

    public static void Save(BoardTheme t)
    {
        PlayerPrefs.SetInt(PrefsKey, (int)t);
        PlayerPrefs.Save();
    }

    public static BoardThemeDef Get(BoardTheme t)
    {
        BoardThemeDef d = new BoardThemeDef();
        d.tiles = new Color[4];
        switch (t)
        {
            case BoardTheme.Emberfall:
                d.displayName = "Emberfall";
                d.tiles[0] = new Color(0.23f, 0.16f, 0.14f); d.tiles[1] = new Color(0.17f, 0.13f, 0.13f);
                d.tiles[2] = new Color(0.28f, 0.17f, 0.12f); d.tiles[3] = new Color(0.20f, 0.14f, 0.15f);
                d.path = new Color(1.00f, 0.46f, 0.10f);
                d.voidC = new Color(0.12f, 0.10f, 0.10f);
                d.start = new Color(0.55f, 0.88f, 0.50f);
                d.end = new Color(1.00f, 0.30f, 0.20f);
                d.swatch = new Color(1.00f, 0.46f, 0.10f);
                break;
            case BoardTheme.Glacier:
                d.displayName = "Glacier";
                d.tiles[0] = new Color(0.74f, 0.86f, 0.94f); d.tiles[1] = new Color(0.64f, 0.79f, 0.90f);
                d.tiles[2] = new Color(0.80f, 0.90f, 0.96f); d.tiles[3] = new Color(0.58f, 0.73f, 0.87f);
                d.path = new Color(0.93f, 0.96f, 1.00f);
                d.voidC = new Color(0.52f, 0.62f, 0.74f);
                d.start = new Color(0.55f, 0.88f, 0.55f);
                d.end = new Color(0.95f, 0.55f, 0.58f);
                d.swatch = new Color(0.64f, 0.82f, 0.95f);
                break;
            case BoardTheme.Mosswood:
                d.displayName = "Mosswood";
                d.tiles[0] = new Color(0.26f, 0.43f, 0.22f); d.tiles[1] = new Color(0.21f, 0.36f, 0.19f);
                d.tiles[2] = new Color(0.31f, 0.50f, 0.24f); d.tiles[3] = new Color(0.24f, 0.40f, 0.26f);
                d.path = new Color(0.50f, 0.38f, 0.24f);
                d.voidC = new Color(0.16f, 0.22f, 0.14f);
                d.start = new Color(0.55f, 0.88f, 0.50f);
                d.end = new Color(0.95f, 0.45f, 0.35f);
                d.swatch = new Color(0.30f, 0.55f, 0.25f);
                break;
            case BoardTheme.DuneSea:
                d.displayName = "Dune Sea";
                d.tiles[0] = new Color(0.87f, 0.76f, 0.58f); d.tiles[1] = new Color(0.80f, 0.68f, 0.50f);
                d.tiles[2] = new Color(0.91f, 0.81f, 0.63f); d.tiles[3] = new Color(0.76f, 0.63f, 0.46f);
                d.path = new Color(0.75f, 0.42f, 0.25f);
                d.voidC = new Color(0.55f, 0.44f, 0.32f);
                d.start = new Color(0.30f, 0.80f, 0.75f);
                d.end = new Color(0.95f, 0.35f, 0.25f);
                d.swatch = new Color(0.85f, 0.65f, 0.35f);
                break;
            case BoardTheme.MidnightCircuit:
                d.displayName = "Midnight Circuit";
                d.tiles[0] = new Color(0.05f, 0.07f, 0.12f); d.tiles[1] = new Color(0.04f, 0.05f, 0.09f);
                d.tiles[2] = new Color(0.08f, 0.06f, 0.14f); d.tiles[3] = new Color(0.05f, 0.08f, 0.13f);
                d.path = new Color(0.10f, 0.80f, 1.00f);
                d.voidC = new Color(0.02f, 0.03f, 0.05f);
                d.start = new Color(1.00f, 0.20f, 0.85f);
                d.end = new Color(1.00f, 0.22f, 0.28f);
                d.flatPath = true;
                d.glowPath = true;
                d.swatch = new Color(0.10f, 0.75f, 1.00f);
                break;
            case BoardTheme.Chocolatier:
                d.displayName = "Chocolatier";
                d.tiles[0] = new Color(0.95f, 0.90f, 0.82f); d.tiles[1] = new Color(0.90f, 0.83f, 0.73f);
                d.tiles[2] = new Color(0.96f, 0.87f, 0.78f); d.tiles[3] = new Color(0.88f, 0.78f, 0.68f);
                d.path = new Color(0.45f, 0.28f, 0.15f);
                d.voidC = new Color(0.70f, 0.58f, 0.45f);
                d.start = new Color(0.55f, 0.90f, 0.70f);
                d.end = new Color(0.95f, 0.35f, 0.45f);
                d.swatch = new Color(0.55f, 0.33f, 0.18f);
                break;
            case BoardTheme.Blueprint:
                d.displayName = "Blueprint";
                d.tiles[0] = new Color(0.16f, 0.36f, 0.66f); d.tiles[1] = new Color(0.13f, 0.30f, 0.58f);
                d.tiles[2] = new Color(0.19f, 0.41f, 0.71f); d.tiles[3] = new Color(0.15f, 0.33f, 0.61f);
                d.path = new Color(0.90f, 0.93f, 1.00f);
                d.voidC = new Color(0.09f, 0.20f, 0.42f);
                d.start = new Color(0.55f, 0.90f, 0.55f);
                d.end = new Color(1.00f, 0.45f, 0.45f);
                d.flatPath = true;
                d.swatch = new Color(0.20f, 0.45f, 0.85f);
                break;
            default: // Classic — the original kid's play-mat
                d.displayName = "Classic";
                d.tiles[0] = new Color(0.82f, 0.34f, 0.34f); d.tiles[1] = new Color(0.34f, 0.56f, 0.86f);
                d.tiles[2] = new Color(0.88f, 0.76f, 0.30f); d.tiles[3] = new Color(0.42f, 0.76f, 0.44f);
                d.path = Color.white;
                d.voidC = new Color(0.60f, 0.54f, 0.44f);
                d.start = new Color(0.25f, 0.80f, 0.35f);
                d.end = new Color(0.85f, 0.22f, 0.22f);
                d.swatch = new Color(0.42f, 0.76f, 0.44f);
                break;
        }
        return d;
    }

    /// <summary>Emissive Standard material for glowing routes ( Finds the same
    /// shader TDVisuals uses; falls back to a plain mat when missing).</summary>
    public static Material GlowMat(Color c, float emit)
    {
        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
        Material m = new Material(sh);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.3f);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emit);
        m.EnableKeyword("_EMISSION");
        return m;
    }
}
