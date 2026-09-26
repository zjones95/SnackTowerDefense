using UnityEngine;

// Renders a couple of towers and a mob on the tiled board so ground contact can
// be checked (they should sit ON the tiles, not sink into them).
public static class TDPlacementPreview
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

        GameObject world = new GameObject("World");
        TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, Vector3.zero);
        TDBoardBuilder.BuildTiles(world.transform, map);
        TDBoardBuilder.BuildRoom(world.transform, map, Vector3.zero);

        // towers on buildable cells (row 1 is "mttmtmmmm" -> x=1 and x=2)
        Vector2Int[] cells = { new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) };
        TowerType[] kinds = { TowerType.SingleShot, TowerType.Sniper, TowerType.Poison };
        for (int i = 0; i < cells.Length; i++)
        {
            GameObject go = new GameObject("T" + i);
            go.transform.position = map.CellCenter(cells[i].x, cells[i].y);
            Tower t = go.AddComponent<Tower>();
            t.Setup(kinds[i], 1, cells[i].x, cells[i].y);
        }

        // a mob standing on the path
        Vector2Int p = TDGameManager.Route[1];
        GameObject mobGO = new GameObject("Mob");
        Vector3 mp = map.CellCenter(p.x, p.y);
        mobGO.transform.position = mp;
        Mob m = mobGO.AddComponent<Mob>();
        m.Init(MobCatalog.Get("Apple"), map.Waypoints, null, 1f, 1f);
        mobGO.transform.position = mp;

        // low-ish angle so the ground contact is obvious
        Vector3 focus = map.CellCenter(1, 3);
        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.transform.position = focus + new Vector3(5.0f, 6.0f, -9.0f);
        cam.transform.LookAt(focus + new Vector3(0f, 0.2f, 0f));

        int w = 1100, h = 800;
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
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "placement.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log("TDPlacementPreview: wrote placement.png (" + png.Length + " bytes)");
    }
}
