using UnityEngine;

/// <summary>
/// Builds the static environment of a board (play-mat tiles + the child's room)
/// so the same code can produce the local board and inert remote boards.
/// Layout/Route are never changed here — themes only reskin tiles + room.
/// </summary>
public enum BoardTheme
{
    Bedroom,
    ArcticOutpost,
    VolcanicCaldera,
    SpaceStation,
    DesertHighway,
    CandyShop,
    SewerSubway,
    MedievalCastle,
    FactoryFloor,
    SunkenReef,
    ZenGarden,
    ClassroomDesk
}

public static class TDBoardBuilder
{
    /// <summary>Themes offered by the single-player map picker, in cycle order.</summary>
    public static readonly BoardTheme[] PickerThemes =
    {
        BoardTheme.Bedroom,
        BoardTheme.ArcticOutpost,
        BoardTheme.VolcanicCaldera,
        BoardTheme.SpaceStation,
        BoardTheme.DesertHighway,
        BoardTheme.CandyShop,
        BoardTheme.SewerSubway,
        BoardTheme.MedievalCastle,
        BoardTheme.FactoryFloor,
        BoardTheme.SunkenReef,
        BoardTheme.ZenGarden,
        BoardTheme.ClassroomDesk
    };

    public static string ThemeName(BoardTheme t)
    {
        switch (t)
        {
            case BoardTheme.ArcticOutpost: return "Arctic Outpost";
            case BoardTheme.VolcanicCaldera: return "Volcanic Caldera";
            case BoardTheme.SpaceStation: return "Space Station";
            case BoardTheme.DesertHighway: return "Desert Highway";
            case BoardTheme.CandyShop: return "Candy Shop";
            case BoardTheme.SewerSubway: return "Sewer / Subway";
            case BoardTheme.MedievalCastle: return "Medieval Castle";
            case BoardTheme.FactoryFloor: return "Factory Floor";
            case BoardTheme.SunkenReef: return "Sunken Reef";
            case BoardTheme.ZenGarden: return "Zen Rock Garden";
            case BoardTheme.ClassroomDesk: return "Classroom Desk";
            default: return "Kid's Bedroom";
        }
    }

    public static string ThemeBlurb(BoardTheme t)
    {
        switch (t)
        {
            case BoardTheme.ArcticOutpost:
                return "Snowfield research outpost - a packed ski-track route over frosted ice pads.";
            case BoardTheme.VolcanicCaldera:
                return "A glowing lava channel threads cooled basalt slabs inside a crater.";
            case BoardTheme.SpaceStation:
                return "A lit mag-rail lane crosses the deck plates of an orbital station.";
            case BoardTheme.DesertHighway:
                return "An asphalt highway threads sun-baked paving across red mesa country.";
            case BoardTheme.CandyShop:
                return "An icing-topped chocolate lane winds between pastel candy slabs.";
            case BoardTheme.SewerSubway:
                return "A steel rail line runs through a flooded concrete undercroft.";
            case BoardTheme.MedievalCastle:
                return "A cobbled road crosses heraldic banners in a torchlit courtyard.";
            case BoardTheme.FactoryFloor:
                return "A conveyor belt snakes between safety-painted machine plates.";
            case BoardTheme.SunkenReef:
                return "A plank wreck walkway crosses the coral beds of a sunken reef.";
            case BoardTheme.ZenGarden:
                return "Stepping stones cross raked gravel in a quiet temple garden.";
            case BoardTheme.ClassroomDesk:
                return "Ruled notebook paper threads between bright sticky notes.";
            default:
                return "The original play-mat: a toy train track weaving through a kid's bedroom.";
        }
    }

    /// <summary>Next/previous theme in the picker ring (wraps).</summary>
    public static BoardTheme NextTheme(BoardTheme t) { return ShiftTheme(t, 1); }
    public static BoardTheme PrevTheme(BoardTheme t) { return ShiftTheme(t, -1); }

    static BoardTheme ShiftTheme(BoardTheme t, int dir)
    {
        int n = PickerThemes.Length;
        int i = System.Array.IndexOf(PickerThemes, t);
        if (i < 0) i = 0;
        return PickerThemes[((i + dir) % n + n) % n];
    }

    /// <summary>Map whose world origin sits at <paramref name="boardOffset"/>.</summary>
    public static TDMap CreateMap(string[] layout, Vector2Int[] route, float cell, Vector3 boardOffset)
    {
        int gw = layout[0].Length, gh = layout.Length;
        Vector3 origin = new Vector3(-gw * cell * 0.5f, 0f, -gh * cell * 0.5f) + boardOffset;
        return new TDMap(layout, route, cell, origin);
    }

    public static void BuildTiles(Transform parent, TDMap map, BoardTheme theme = BoardTheme.Bedroom)
    {
        string[] layout = null;
        // rebuild the char grid from the map (layout is cheap and keeps callers simple)
        int gw = map.Width, gh = map.Height;
        layout = new string[gh];
        for (int ly = 0; ly < gh; ly++)
        {
            char[] row = new char[gw];
            for (int x = 0; x < gw; x++) row[x] = map.At(x, ly);
            layout[ly] = new string(row);
        }

        if (theme == BoardTheme.ArcticOutpost) { BuildTilesArctic(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.VolcanicCaldera) { BuildTilesVolcanic(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.SpaceStation) { BuildTilesStation(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.DesertHighway) { BuildTilesDesert(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.CandyShop) { BuildTilesCandy(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.SewerSubway) { BuildTilesSewer(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.MedievalCastle) { BuildTilesCastle(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.FactoryFloor) { BuildTilesFactory(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.SunkenReef) { BuildTilesReef(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.ZenGarden) { BuildTilesZen(parent, map, layout, gw, gh); return; }
        if (theme == BoardTheme.ClassroomDesk) { BuildTilesClassroom(parent, map, layout, gw, gh); return; }

        Color[] tileCols =
        {
            new Color(0.82f, 0.34f, 0.34f),
            new Color(0.34f, 0.56f, 0.86f),
            new Color(0.88f, 0.76f, 0.30f),
            new Color(0.42f, 0.76f, 0.44f)
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Weave(), tileCols[i], new Vector2(3f, 3f));

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Road(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.RoadCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.60f, 0.54f, 0.44f), 0f, 0.25f);
        Material startMat = TDVisuals.Mat(new Color(0.25f, 0.80f, 0.35f), 0f, 0.3f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.22f, 0.22f), 0f, 0.3f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;   // top face flush with y = 0
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    /// <summary>Room is authored around the grid centre, so parent it to a root at the offset.</summary>
    public static GameObject BuildRoom(Transform parent, TDMap map, Vector3 boardOffset, BoardTheme theme = BoardTheme.Bedroom)
    {
        GameObject root = new GameObject("Room");
        root.transform.SetParent(parent, false);
        root.transform.position = boardOffset;
        TDRoom.Build(root.transform, map, theme);
        return root;
    }

    static void BuildTilesArctic(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.38f, 0.52f, 0.64f),
            new Color(0.43f, 0.57f, 0.69f),
            new Color(0.34f, 0.47f, 0.60f),
            new Color(0.48f, 0.61f, 0.72f)
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Frost(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.SnowTrack(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.SnowCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.80f, 0.85f, 0.90f), 0f, 0.25f);
        Material startMat = TDVisuals.Mat(new Color(0.55f, 0.85f, 0.65f), 0f, 0.3f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.38f, 0.36f), 0f, 0.3f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesVolcanic(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        // Plots are dark cooled basalt; the path is the glowing lava channel,
        // so the walkable route is by far the brightest thing on the board.
        Color[] tileCols =
        {
            new Color(0.30f, 0.28f, 0.30f),
            new Color(0.25f, 0.24f, 0.27f),
            new Color(0.35f, 0.32f, 0.33f),
            new Color(0.22f, 0.21f, 0.24f)
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Basalt(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.LavaFlow(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.LavaCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.14f, 0.13f, 0.14f), 0f, 0.2f);
        Material startMat = TDVisuals.Mat(new Color(0.30f, 0.85f, 0.38f), 0f, 0.35f);
        Material endMat = TDVisuals.Mat(new Color(0.95f, 0.30f, 0.10f), 0f, 0.35f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesStation(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        // Three-level read: glowing mag-rail path > deck plates > recessed dark floor.
        Color[] tileCols =
        {
            new Color(0.38f, 0.42f, 0.50f),
            new Color(0.34f, 0.38f, 0.46f),
            new Color(0.42f, 0.46f, 0.54f),
            new Color(0.30f, 0.34f, 0.42f)
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.DeckPanel(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.MagRail(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.MagRailCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.15f, 0.17f, 0.21f), 0.1f, 0.35f);   // recessed floor
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.95f, 0.55f), 0f, 0.4f);     // airlock
        Material endMat = TDVisuals.Mat(new Color(0.95f, 0.25f, 0.22f), 0f, 0.4f);       // warning light

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesDesert(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        // Paving plots are deliberately LIGHT and the asphalt path deliberately
        // dark, so the white lane markings carry the route read.
        Color[] tileCols =
        {
            new Color(0.92f, 0.78f, 0.56f),   // tan paving
            new Color(0.80f, 0.48f, 0.32f),   // terracotta
            new Color(0.42f, 0.72f, 0.72f),   // turquoise tile
            new Color(0.90f, 0.84f, 0.68f)    // pale sandstone
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Adobe(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Asphalt(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.AsphaltCross(), Color.white, Vector2.one);
        // void is loose sand, not paving: flat (no seams) and a touch duller
        Material voidMat = TDVisuals.Mat(new Color(0.82f, 0.70f, 0.50f), 0f, 0.3f);
        Material startMat = TDVisuals.Mat(new Color(0.32f, 0.82f, 0.36f), 0f, 0.4f);
        Material endMat = TDVisuals.Mat(new Color(0.88f, 0.22f, 0.18f), 0f, 0.4f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesCandy(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        // Pastel candy plots against a dark chocolate lane: same dark-path read
        // that worked on the desert board.
        Color[] tileCols =
        {
            new Color(0.98f, 0.62f, 0.70f),   // strawberry
            new Color(0.62f, 0.78f, 0.98f),   // blueberry
            new Color(0.98f, 0.90f, 0.55f),   // lemon
            new Color(0.62f, 0.92f, 0.70f)    // mint
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Candy(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Chocolate(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.ChocolateCross(), Color.white, Vector2.one);
        // void is plain wrapped toffee paper: flat, no candy sheen
        Material voidMat = TDVisuals.Mat(new Color(0.88f, 0.83f, 0.74f), 0f, 0.3f);
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.85f, 0.40f), 0f, 0.5f);   // gumdrop
        Material endMat = TDVisuals.Mat(new Color(0.88f, 0.18f, 0.20f), 0f, 0.5f);     // licorice

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesSewer(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.80f, 0.80f, 0.78f),   // damp concrete
            new Color(0.95f, 0.82f, 0.30f),   // hazard yellow
            new Color(0.92f, 0.58f, 0.26f),   // caution orange
            new Color(0.58f, 0.62f, 0.44f)    // algae olive
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Concrete(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.RailTrack(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.RailCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.34f, 0.34f, 0.33f), 0.05f, 0.4f);   // wet concrete
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.82f, 0.45f), 0f, 0.45f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.25f, 0.22f), 0f, 0.45f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesCastle(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.78f, 0.24f, 0.24f),   // deep red banner
            new Color(0.24f, 0.40f, 0.74f),   // royal blue
            new Color(0.88f, 0.72f, 0.26f),   // gold
            new Color(0.28f, 0.55f, 0.32f)    // forest green
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Banner(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Cobble(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.CobbleCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.60f, 0.50f, 0.36f), 0f, 0.3f);     // packed earth
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.72f, 0.38f), 0f, 0.4f);
        Material endMat = TDVisuals.Mat(new Color(0.78f, 0.22f, 0.20f), 0f, 0.4f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesFactory(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.34f, 0.52f, 0.80f),   // machine blue
            new Color(0.92f, 0.80f, 0.26f),   // safety yellow
            new Color(0.62f, 0.64f, 0.66f),   // industrial grey
            new Color(0.32f, 0.56f, 0.40f)    // dark green
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.SafetyPlate(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Conveyor(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.ConveyorCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.48f, 0.50f, 0.53f), 0.3f, 0.5f);   // checker plate
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.80f, 0.40f), 0f, 0.45f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.25f, 0.22f), 0f, 0.45f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesReef(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.98f, 0.55f, 0.58f),   // coral pink
            new Color(0.66f, 0.50f, 0.95f),   // anemone purple
            new Color(0.50f, 0.88f, 0.60f),   // algae green
            new Color(0.97f, 0.96f, 0.92f)    // shell white
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Seabed(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.ShipWreck(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.ShipWreckCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.78f, 0.76f, 0.68f), 0f, 0.3f);     // open seabed sand
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.85f, 0.50f), 0f, 0.45f);
        Material endMat = TDVisuals.Mat(new Color(0.90f, 0.30f, 0.28f), 0f, 0.45f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesZen(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.74f, 0.74f, 0.72f),   // stone grey
            new Color(0.62f, 0.74f, 0.55f),   // moss green
            new Color(0.78f, 0.42f, 0.38f),   // maple red
            new Color(0.90f, 0.82f, 0.56f)    // straw gold
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.RakedGravel(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.SteppingPath(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.SteppingPathCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.90f, 0.89f, 0.86f), 0f, 0.3f);     // open raked gravel
        Material startMat = TDVisuals.Mat(new Color(0.45f, 0.70f, 0.45f), 0f, 0.4f);
        Material endMat = TDVisuals.Mat(new Color(0.76f, 0.28f, 0.26f), 0f, 0.4f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static void BuildTilesClassroom(Transform parent, TDMap map, string[] layout, int gw, int gh)
    {
        Color[] tileCols =
        {
            new Color(0.98f, 0.88f, 0.35f),   // sticky yellow
            new Color(0.98f, 0.55f, 0.68f),   // sticky pink
            new Color(0.45f, 0.72f, 0.95f),   // sticky blue
            new Color(0.55f, 0.88f, 0.52f)    // sticky green
        };
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.NotePaper(), tileCols[i], Vector2.one);

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Notebook(), Color.white, Vector2.one);
        Material crossMat = TDVisuals.TexturedMat(TDTextures.NotebookCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.80f, 0.75f, 0.66f), 0f, 0.3f);     // kraft desk paper
        Material startMat = TDVisuals.Mat(new Color(0.35f, 0.80f, 0.45f), 0f, 0.4f);
        Material endMat = TDVisuals.Mat(new Color(0.88f, 0.28f, 0.26f), 0f, 0.4f);

        float cell = map.Cell;
        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) - Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    int o = PathOrientation(map, x, ly);
                    GameObject tile = TDVisuals.Box(parent, "Tile", pos, scale, o == 2 ? crossMat : pathMat);
                    if (o == 1) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(parent, "Tile", pos, scale, m);
                }
            }
        }
    }

    static int PathOrientation(TDMap map, int x, int ly)
    {
        bool l = map.IsPath(x - 1, ly), r = map.IsPath(x + 1, ly);
        bool u = map.IsPath(x, ly - 1), d = map.IsPath(x, ly + 1);
        bool horiz = l || r, vert = u || d;
        if (horiz && vert) return 2;
        if (vert) return 1;
        return 0;
    }
}
