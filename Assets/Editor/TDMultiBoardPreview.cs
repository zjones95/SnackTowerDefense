using UnityEngine;

// Renders several boards laid out as they would be in a multiplayer match, so
// spacing/overlap can be checked without starting a session.
public static class TDMultiBoardPreview
{
    public static void Render()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);

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

        // four boards in a row (worst case for width)
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            Vector3 off = BoardLayout.Position(i, count);
            GameObject go = new GameObject("Board" + i);
            TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, off);
            TDBoardBuilder.BuildTiles(go.transform, map);
            TDBoardBuilder.BuildRoom(go.transform, map, off);
        }

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 62f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.transform.position = new Vector3(-40f, 80f, -85f);
        cam.transform.LookAt(new Vector3(0f, 0f, 0f));

        int w = 1600, h = 700;
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
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "multiboard.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log("TDMultiBoardPreview: wrote multiboard.png (" + png.Length + " bytes)");
    }
}
