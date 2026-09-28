using System;
using System.IO;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod RemainingTowerPreview.Render -quit
// Checks the actual runtime composition, including viewer-height grounding and aiming.
public static class RemainingTowerPreview
{
    static readonly TowerType[] Types = {
        TowerType.Slow, TowerType.IceCreamTruck, TowerType.FondueFountain,
        TowerType.Gold, TowerType.BobaBlaster, TowerType.PizzaOven
    };

    public static void Render()
    {
        int checkedModels = 0;
        foreach (TowerType type in Types)
        {
            if (SnackModels.Load(SnackModels.TowerPath(type)) == null)
                throw new Exception("Missing model: " + type);
            foreach (float stageY in new[] { 0f, -400f })
            for (int tier = 1; tier <= 7; tier++)
            {
                GameObject root = new GameObject("Verify_" + type);
                root.transform.position = new Vector3(4f, stageY, 3f);
                Transform head = TowerVisual.Build(root.transform, type, tier);
                Renderer rim = Array.Find(root.GetComponentsInChildren<Renderer>(), r => r.name == "rim");
                if (rim == null || rim.transform.IsChildOf(head))
                    throw new Exception("Missing or rotating base: " + type);
                Bounds before = rim.bounds;
                if (Mathf.Abs(before.min.y - stageY) > 0.0001f)
                    throw new Exception("Sinking base: " + type + " tier " + tier);
                if (Mathf.Abs(before.center.x - 4f) > 0.0001f || Mathf.Abs(before.center.z - 3f) > 0.0001f)
                    throw new Exception("Off-center base: " + type);
                if (head.GetComponentsInChildren<Renderer>().Length == 0)
                    throw new Exception("Empty rotating head: " + type);
                head.localRotation = Quaternion.Euler(0f, 93f, 0f);
                if ((rim.bounds.center - before.center).sqrMagnitude > 0.0000001f)
                    throw new Exception("Base moved with aim: " + type);
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                    foreach (Material material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported)
                            throw new Exception("Invalid material: " + type);
                UnityEngine.Object.DestroyImmediate(root);
                checkedModels++;
            }
        }

        Light light = new GameObject("Preview key").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = .9f;
        light.transform.rotation = Quaternion.Euler(45f, 145f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.28f, .28f, .3f);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -.1f, 1f);
        floor.transform.localScale = new Vector3(6f, .2f, 5f);
        floor.GetComponent<Renderer>().sharedMaterial = TDVisuals.Mat(new Color(.12f, .17f, .22f), 0f, .2f);
        for (int i = 0; i < Types.Length; i++)
        {
            GameObject root = new GameObject(Types[i].ToString());
            root.transform.position = new Vector3((i % 3 - 1) * 1.65f, 0f, (1 - i / 3) * 1.8f);
            TowerVisual.Build(root.transform, Types[i], 2); // identical scale for art comparison
        }
        Camera camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.1f;
        camera.transform.position = new Vector3(-2.6f, 5.3f, 7.8f);
        camera.transform.LookAt(new Vector3(0f, .45f, .85f));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.12f, .17f, .22f);
        RenderTexture target = new RenderTexture(1600, 1100, 24);
        target.antiAliasing = 4;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1600, 1100, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 1100), 0, 0);
        image.Apply();
        string path = Path.Combine(Path.GetTempPath(), "opencode", "remaining_towers_unity.png");
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("RemainingTowerPreview: PASS " + checkedModels + " grounding/aim/material checks; wrote " + path);
    }
}
