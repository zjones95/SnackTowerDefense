using UnityEngine;

// Editor-only preview renderer for the snack art. Run with:
//   Unity -batchmode -projectPath <proj> -executeMethod SnackPreview.Render -quit
public static class SnackPreview
{
    public static void Render()
    {        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);

        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.90f, 0.74f);
        l.intensity = 1.15f;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.26f, 0.21f, 0.17f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(36f, 1f, 18f);
        ground.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(0.22f, 0.28f, 0.22f), 0f, 0.2f);

        TowerType[] types = TowerCatalog.AllTypes;
        for (int i = 0; i < types.Length; i++)
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                GameObject go = new GameObject("T_" + types[i] + "_" + tier);
                go.transform.position = new Vector3(-10.5f + i * 3f, 0f, 2.6f - (tier - 1) * 2.8f);
                Tower t = go.AddComponent<Tower>();
                t.Setup(types[i], tier, 0, 0);
            }
        }

        string[] mobs = { "Apple", "GranolaMom", "Potato", "Watermelon" };
        for (int i = 0; i < mobs.Length; i++)
        {
            MobDef def = MobCatalog.Get(mobs[i]);
            GameObject go = new GameObject("M_" + mobs[i]);
            go.transform.position = new Vector3(-3.9f + i * 2.6f, 0f, -5.6f);
            GameObject prefab = SnackModels.Load(MobCatalog.ModelPath(def));
            if (prefab != null)
            {
                GameObject m = Object.Instantiate(prefab, go.transform);
                m.name = "Model";
                m.transform.localPosition = Vector3.zero;
                m.transform.localScale = Vector3.one * (def.scale * 1.5f);
                SnackModels.CenterOn(m, go.transform.position);
            }
            else
            {
                ChildModel cm = go.AddComponent<ChildModel>();
                Color pants = new Color(def.color.r * 0.45f, def.color.g * 0.45f, def.color.b * 0.55f);
                cm.Build(def.color, pants, new Color(0.95f, 0.78f, 0.62f), new Color(0.25f, 0.15f, 0.09f), def.scale);
                cm.PoseForPreview(i * 1.1f);
            }
        }

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.075f, 0.065f, 0.06f);
        cam.transform.position = new Vector3(-13f, 15f, -19f);
        cam.transform.LookAt(new Vector3(0f, 0.4f, 0.0f));

        int w = 1400, h = 820;
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
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "snack_preview.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        Debug.Log("SnackPreview: wrote snack_preview.png (" + png.Length + " bytes)");
    }

    // Close-up of the tier labels so the digit/plate fit can be judged.
    public static void RenderClose()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode");
        System.IO.Directory.CreateDirectory(dir);

        GameObject lightGO = new GameObject("PreviewLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.90f, 0.74f);
        l.intensity = 1.15f;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.26f, 0.21f, 0.17f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(0.22f, 0.28f, 0.22f), 0f, 0.2f);

        for (int tier = 1; tier <= 3; tier++)
        {
            GameObject go = new GameObject("T_" + tier);
            go.transform.position = new Vector3((tier - 2) * 2.4f, 0f, 0f);
            Tower t = go.AddComponent<Tower>();
            t.Setup(TowerCatalog.AllTypes[0], tier, 0, 0);
        }

        GameObject camGO = new GameObject("PreviewCam");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2.4f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.075f, 0.065f, 0.06f);
        cam.transform.position = new Vector3(0f, 2.4f, -6.5f);
        cam.transform.LookAt(new Vector3(0f, 1.5f, 0f));

        int w = 1400, h = 760;
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
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "snack_close.png"), png);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        Debug.Log("SnackPreview: wrote snack_close.png (" + png.Length + " bytes)");
    }

    public static void Probe()
    {
        string[] paths =
        {
            "Snack/Towers/Popcorn", "Snack/Towers/Soda", "Snack/Towers/Gum", "Snack/Towers/Pretzel",
            "Snack/Towers/Chain", "Snack/Towers/Pierce", "Snack/Towers/Wasabi",
            "Snack/Mobs/Cracker"
        };
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject g = Resources.Load<GameObject>(paths[i]);
            Debug.Log("PROBE " + paths[i] + " -> " + (g != null ? g.name : "NULL"));
        }
    }
}
