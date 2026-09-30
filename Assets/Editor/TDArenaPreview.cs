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

        // Batch mode never runs Awake, so ActiveTheme sits at its declared default.
        // TD_PREVIEW_THEME lets a headless render target any board without a rebuild:
        //   TD_PREVIEW_THEME=SpaceStation  ->  map_preview.png for that theme.
        string themeEnv = System.Environment.GetEnvironmentVariable("TD_PREVIEW_THEME");
        if (!string.IsNullOrEmpty(themeEnv))
        {
            try { TDGameManager.ActiveTheme = (BoardTheme)System.Enum.Parse(typeof(BoardTheme), themeEnv, true); }
            catch { Debug.LogWarning("TDArenaPreview: unknown TD_PREVIEW_THEME '" + themeEnv + "', using " + TDGameManager.ActiveTheme); }
        }

        // Same per-theme light table the game uses, so the preview matches a run.
        Color keyCol, fillCol, ambCol;
        float keyI, fillI;
        switch (TDGameManager.ActiveTheme)
        {
            case BoardTheme.ArcticOutpost:
                keyCol = new Color(0.86f, 0.92f, 1.00f); keyI = 0.85f;
                fillCol = new Color(0.60f, 0.70f, 0.90f); fillI = 0.30f;
                ambCol = new Color(0.17f, 0.19f, 0.22f);
                break;
            case BoardTheme.VolcanicCaldera:
                keyCol = new Color(1.00f, 0.72f, 0.48f); keyI = 0.78f;
                fillCol = new Color(0.95f, 0.32f, 0.10f); fillI = 0.22f;
                ambCol = new Color(0.14f, 0.10f, 0.09f);
                break;
            case BoardTheme.SpaceStation:
                keyCol = new Color(0.72f, 0.88f, 1.00f); keyI = 0.75f;
                fillCol = new Color(0.25f, 0.45f, 0.85f); fillI = 0.32f;
                ambCol = new Color(0.13f, 0.16f, 0.22f);
                break;
            case BoardTheme.DesertHighway:
                keyCol = new Color(1.00f, 0.95f, 0.80f); keyI = 1.05f;
                fillCol = new Color(0.88f, 0.74f, 0.55f); fillI = 0.22f;
                ambCol = new Color(0.26f, 0.24f, 0.20f);
                break;
            default:
                keyCol = new Color(1.00f, 0.86f, 0.66f); keyI = 0.72f;
                fillCol = new Color(0.55f, 0.62f, 0.78f); fillI = 0.10f;
                ambCol = new Color(0.12f, 0.11f, 0.10f);
                break;
        }

        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = keyCol;
        l.intensity = keyI;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(50f, 35f, 0f);

        GameObject fillGO = new GameObject("PreviewFill");
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = fillCol;
        fill.intensity = fillI;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambCol;
        RenderSettings.ambientIntensity = 1f;

        GameObject boardRoot = new GameObject("BoardRoot");
        TDBoardBuilder.BuildTiles(boardRoot.transform, map, TDGameManager.ActiveTheme);
        TDRoom.Build(boardRoot.transform, map, TDGameManager.ActiveTheme);

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
