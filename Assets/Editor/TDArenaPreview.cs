using UnityEngine;

// Editor preview that renders the current grid map with its tile textures.
public static class TDArenaPreview
{
    public static void Render()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);

        string[] layout = TDGameManager.Layout;
        Vector2Int[] route = TDGameManager.Route;
        float cell = 2f;
        int gw = layout[0].Length, gh = layout.Length;
        TDMap map = new TDMap(layout, route, cell, new Vector3(-gw * cell * 0.5f, 0f, -gh * cell * 0.5f));

        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.86f, 0.66f);
        l.intensity = 0.72f;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(50f, 35f, 0f);

        GameObject fillGO = new GameObject("PreviewFill");
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.55f, 0.62f, 0.78f);
        fill.intensity = 0.10f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.11f, 0.10f);

        // colourful play-mat tiles
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

        Material pathMat = TDVisuals.TexturedMat(TDTextures.Road(), Color.white, Vector2.one);        // toy train track
        Material crossMat = TDVisuals.TexturedMat(TDTextures.RoadCross(), Color.white, Vector2.one);
        Material voidMat = TDVisuals.Mat(new Color(0.60f, 0.54f, 0.44f), 0f, 0.25f);
        Material startMat = TDVisuals.Mat(new Color(0.25f, 0.80f, 0.35f), 0f, 0.3f);
        Material endMat = TDVisuals.Mat(new Color(0.85f, 0.22f, 0.22f), 0f, 0.3f);

        for (int ly = 0; ly < gh; ly++)
        {
            for (int x = 0; x < gw; x++)
            {
                char c = layout[ly][x];
                Vector3 pos = map.CellCenter(x, ly) + Vector3.up * 0.05f;
                Vector3 scale = new Vector3(cell * 0.97f, 0.10f, cell * 0.97f);
                if (c == 'm')
                {
                    bool left = map.IsPath(x - 1, ly), right = map.IsPath(x + 1, ly);
                    bool up = map.IsPath(x, ly - 1), down = map.IsPath(x, ly + 1);
                    bool horiz = left || right, vert = up || down;
                    Material pm = (horiz && vert) ? crossMat : pathMat;
                    GameObject tile = TDVisuals.Box(null, "Tile", pos, scale, pm);
                    if (vert && !horiz) tile.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    Material m = voidMat;
                    if (c == 't') m = tileMats[(x + ly) % tileMats.Length];
                    else if (c == 's') m = startMat;
                    else if (c == 'e') m = endMat;
                    TDVisuals.Box(null, "Tile", pos, scale, m);
                }
            }
        }

        TDRoom.Build(null, map);

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 20f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.transform.position = new Vector3(-20f, 30f, -32f);
        cam.transform.LookAt(new Vector3(0f, 0f, 0f));

        int w = 1000, h = 1000;
        RenderTexture rt = new RenderTexture(w, h, 24);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "map_preview.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log("TDArenaPreview: wrote map_preview.png (" + png.Length + " bytes)");
    }
}
