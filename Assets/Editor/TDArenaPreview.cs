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
        bool arctic = TDGameManager.ActiveTheme == BoardTheme.ArcticOutpost;
        l.color = arctic ? new Color(0.86f, 0.92f, 1.0f) : new Color(1f, 0.86f, 0.66f);
        l.intensity = arctic ? 0.85f : 0.72f;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(50f, 35f, 0f);

        GameObject fillGO = new GameObject("PreviewFill");
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = arctic ? new Color(0.60f, 0.70f, 0.90f) : new Color(0.55f, 0.62f, 0.78f);
        fill.intensity = arctic ? 0.30f : 0.10f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = arctic ? new Color(0.17f, 0.19f, 0.22f) : new Color(0.12f, 0.11f, 0.10f);

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
