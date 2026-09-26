using UnityEngine;

// Renders a RemoteBoard fed a synthetic snapshot, so the spectate render path
// (remote towers, mobs, health bars, projectiles, nameplate) can be checked
// without a live session.
public static class TDSpectatePreview
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

        RemoteBoard rb = RemoteBoard.Create(null, Vector3.zero, 7, "Zach");
        rb.SetStatus("wave 3   |   14 lives");

        TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, Vector3.zero);

        BoardSnapshot snap = new BoardSnapshot();
        snap.Towers.Add(new BoardSnapshot.TowerSnap { Type = (byte)TowerType.SingleShot, Tier = 2, Cx = 1, Cy = 1, Yaw = 0 });
        snap.Towers.Add(new BoardSnapshot.TowerSnap { Type = (byte)TowerType.Sniper, Tier = 3, Cx = 3, Cy = 6, Yaw = 128 });
        snap.Towers.Add(new BoardSnapshot.TowerSnap { Type = (byte)TowerType.Splash, Tier = 1, Cx = 1, Cy = 8, Yaw = 64 });

        Vector2Int[] route = TDGameManager.Route;
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = map.CellCenter(route[i].x, route[i].y);
            snap.Mobs.Add(new BoardSnapshot.MobSnap
            {
                Id = (ushort)(i + 1),
                Type = (byte)(i % 2 == 0 ? 0 : 3),
                X = BoardSnapshot.Enc(p.x),
                Z = BoardSnapshot.Enc(p.z),
                Hp = (byte)(255 - i * 30)
            });
        }
        snap.Projs.Add(new BoardSnapshot.ProjSnap { Id = 1, X = BoardSnapshot.Enc(2f), Y = BoardSnapshot.Enc(1f), Z = BoardSnapshot.Enc(5f) });
        snap.Projs.Add(new BoardSnapshot.ProjSnap { Id = 2, X = BoardSnapshot.Enc(4f), Y = BoardSnapshot.Enc(1.1f), Z = BoardSnapshot.Enc(9f) });

        rb.Apply(snap);

        Transform nameplate = rb.transform.Find("BoardName");
        if (nameplate != null) nameplate.gameObject.SetActive(false);

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 17f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.transform.position = new Vector3(-16f, 24f, -26f);
        cam.transform.LookAt(new Vector3(0f, 0f, 0f));

        int w = 1200, h = 900;
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
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "spectate.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log("TDSpectatePreview: wrote spectate.png (" + png.Length + " bytes)");
    }
}
