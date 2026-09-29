using UnityEngine;

/// <summary>
/// Builds the static environment of a board (play-mat tiles + the child's room)
/// so the same code can produce the local board and inert remote boards.
/// </summary>
public static class TDBoardBuilder
{
    /// <summary>Map whose world origin sits at <paramref name="boardOffset"/>.</summary>
    public static TDMap CreateMap(string[] layout, Vector2Int[] route, float cell, Vector3 boardOffset)
    {
        int gw = layout[0].Length, gh = layout.Length;
        Vector3 origin = new Vector3(-gw * cell * 0.5f, 0f, -gh * cell * 0.5f) + boardOffset;
        return new TDMap(layout, route, cell, origin);
    }

    public static void BuildTiles(Transform parent, TDMap map) { BuildTiles(parent, map, BoardTheme.Classic); }

    public static void BuildTiles(Transform parent, TDMap map, BoardTheme theme)
    {
        BoardThemeDef th = BoardThemes.Get(theme);
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

        Color[] tileCols = th.tiles;
        Material[] tileMats = new Material[tileCols.Length];
        for (int i = 0; i < tileCols.Length; i++)
            tileMats[i] = TDVisuals.TexturedMat(TDTextures.Weave(), tileCols[i], new Vector2(3f, 3f));

        Material pathMat = th.flatPath ? TDVisuals.Mat(th.path, 0f, 0.4f)
                                       : TDVisuals.TexturedMat(TDTextures.Road(), th.path, Vector2.one);
        Material crossMat = th.flatPath ? pathMat
                                        : TDVisuals.TexturedMat(TDTextures.RoadCross(), th.path, Vector2.one);
        if (th.glowPath)
        {
            pathMat = BoardThemes.GlowMat(th.path, 1.1f);
            crossMat = pathMat;
        }
        Material voidMat = TDVisuals.Mat(th.voidC, 0f, 0.25f);
        Material startMat = th.glowPath ? BoardThemes.GlowMat(th.start, 1.3f) : TDVisuals.Mat(th.start, 0f, 0.3f);
        Material endMat = th.glowPath ? BoardThemes.GlowMat(th.end, 1.3f) : TDVisuals.Mat(th.end, 0f, 0.3f);

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
    public static GameObject BuildRoom(Transform parent, TDMap map, Vector3 boardOffset)
    {
        GameObject root = new GameObject("Room");
        root.transform.SetParent(parent, false);
        root.transform.position = boardOffset;
        TDRoom.Build(root.transform, map);
        return root;
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
