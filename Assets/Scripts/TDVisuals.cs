using System.Collections.Generic;
using UnityEngine;

// Small helpers for building the game out of primitives.
public static class TDVisuals
{
    static Shader ShaderFor()
    {
        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
        return sh;
    }

    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static Material Mat(Color c, float metallic = 0f, float smooth = 0.3f)
    {
        string key = c.r + "_" + c.g + "_" + c.b + "_" + c.a + "_" + metallic + "_" + smooth;
        Material cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        Material m = new Material(ShaderFor());
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        cache[key] = m;
        return m;
    }

    // Translucent material (e.g. the tier-number plate). Configures the Standard
    // shader's blend mode, which a plain alpha value alone would not enable.
    public static Material TransparentMat(Color c, float alpha, float smooth = 0.3f)
    {
        Material m = new Material(ShaderFor());
        Color col = new Color(c.r, c.g, c.b, alpha);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);

        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);  // URP: transparent surface
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);      // URP: alpha blend

        m.SetOverrideTag("RenderType", "Transparent");
        if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);        // Standard: transparent
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 2990; // draw before the glyphs so the number stays on top
        return m;
    }

    // Material with a tiled texture (not cached: each texture needs its own).
    public static Material TexturedMat(Texture2D tex, Color tint, Vector2 tiling)
    {
        Material m = new Material(ShaderFor());
        m.mainTexture = tex;
        if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (tex != null) m.mainTextureScale = tiling;
        return m;
    }

    static void Strip(GameObject g)
    {
        Collider c = g.GetComponent<Collider>();
        if (c == null) return;
        if (Application.isPlaying) Object.Destroy(c);
        else Object.DestroyImmediate(c);
    }

    public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Strip(g);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    public static GameObject Sphere(Transform parent, string name, Vector3 pos, float diameter, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Strip(g);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = Vector3.one * diameter;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    public static GameObject Cyl(Transform parent, string name, Vector3 pos, float radius, float height, Material m)    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(g);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    // Capsule-ish limb stretched between two points (arms and legs).
    public static GameObject Limb(Transform parent, string name, Vector3 from, Vector3 to, float radius, Material m)
    {
        Vector3 dir = to - from;
        float len = dir.magnitude;
        if (len < 0.0001f) len = 0.0001f;
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Strip(g);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = from + dir * 0.5f;
        g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir / len);
        g.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }
}
