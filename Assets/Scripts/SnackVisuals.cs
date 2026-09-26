using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the *visual* of a tower (model, rotating head, tier glow) without any
/// gameplay logic, so both live towers and remote/spectated copies share one look.
/// </summary>
public static class TowerVisual
{
    /// <summary>Visual size for a tier: tier 1 is deliberately small, and each
    /// merge step grows the tower so the tiers read at a glance.</summary>
    public static float TierScale(int tier)
    {
        return 0.78f + 0.22f * (Mathf.Clamp(tier, 1, TowerCatalog.MaxTier) - 1);
    }

    /// <summary>Creates the tower model and returns the rotating head pivot.</summary>
    public static Transform Build(Transform parent, TowerType type, int tier)
    {
        Transform turret;

        // One scaled root holds the whole model, so a tier change resizes the
        // model, its muzzle height and the tier badge together.
        GameObject modelRoot = new GameObject("ModelRoot");
        modelRoot.transform.SetParent(parent, false);
        modelRoot.transform.localScale = Vector3.one * TierScale(tier);

        GameObject prefab = SnackModels.Load(SnackModels.TowerPath(type));
        if (prefab != null)
        {
            GameObject model = Object.Instantiate(prefab, modelRoot.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            SnackModels.CenterOn(model, modelRoot.transform.position);

            Transform pivot = new GameObject("HeadPivot").transform;
            pivot.SetParent(modelRoot.transform, false);
            List<Transform> heads = new List<Transform>();
            foreach (Transform tr in model.GetComponentsInChildren<Transform>())
            {
                if (tr == model.transform) continue;
                string n = tr.name.ToLower();
                if (n.Contains("pedestal") || n.Contains("rim")) continue;
                heads.Add(tr);
            }
            foreach (Transform h in heads) h.SetParent(pivot, true);
            turret = pivot;
        }
        else
        {
            GameObject tg = new GameObject("Turret");
            tg.transform.SetParent(modelRoot.transform, false);
            tg.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            turret = tg.transform;
            SnackArt.BuildTower(modelRoot.transform, turret, type, tier);
        }

        BuildTierGlow(parent, tier);

        return turret;
    }

    /// <summary>Tier palette — the same three colours for every tower type.</summary>
    public static readonly Color[] TierColours =
    {
        new Color(0.80f, 0.82f, 0.86f),  // 1 - grey
        new Color(0.30f, 0.60f, 1.00f),  // 2 - blue
        new Color(0.30f, 0.92f, 0.42f)   // 3 - green
    };

    public static Color TierColour(int tier)
    {
        return TierColours[Mathf.Clamp(tier, 1, TierColours.Length) - 1];
    }

    /// <summary>Additive billboard flare tinted by tier — soft, bright and
    /// see-through, so it reads as light rather than a solid shape. Same palette
    /// for every tower type.</summary>
    static void BuildTierGlow(Transform parent, int tier)
    {
        Renderer[] rs = parent.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return;

        Material m = GlowMat(TierColour(tier));
        if (m == null) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float size = Mathf.Max(1.0f, b.size.y * 1.5f);
        GameObject quad = TDVisuals.Quad(parent, "TierGlow", Vector3.zero, size, m);
        TierGlow tg = quad.AddComponent<TierGlow>();
        tg.Center = parent.InverseTransformPoint(b.center);
        tg.Push = Mathf.Max(0.15f, b.extents.magnitude * 0.75f);   // clear of its own model
        tg.FaceNow();                                              // correct on frame one
    }

    const float GlowIntensity = 0.45f;

    static readonly Dictionary<Color, Material> glowMats = new Dictionary<Color, Material>();
    static Texture2D glowTex;

    static Material GlowMat(Color c)
    {
        Material m;
        if (glowMats.TryGetValue(c, out m) && m != null) return m;

        Shader sh = Resources.Load<Shader>("Snack/Glow");
        if (sh == null) sh = Shader.Find("Snack/Glow");
        if (sh == null) return null;

        m = new Material(sh);
        m.mainTexture = GlowTexture();
        m.SetColor("_Color", new Color(c.r, c.g, c.b, GlowIntensity));
        glowMats[c] = m;
        return m;
    }

    /// <summary>A soft ambient radial glow (no rays), generated once at runtime.</summary>
    static Texture2D GlowTexture()
    {
        if (glowTex != null) return glowTex;

        const int n = 256;
        glowTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        glowTex.name = "TierGlowTex";
        glowTex.wrapMode = TextureWrapMode.Clamp;
        glowTex.filterMode = FilterMode.Bilinear;

        float c0 = (n - 1) * 0.5f;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x - c0) / c0;
                float dy = (y - c0) / c0;
                float r2 = dx * dx + dy * dy;   // squared radius

                // tight falloff that dies out well before the quad edge, so there
                // is no hard cut-off where the texture ends
                float glow = Mathf.Exp(-r2 * 5.5f) + Mathf.Exp(-r2 * 20f) * 0.15f;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(glow));
            }
        }
        glowTex.SetPixels(px);
        glowTex.Apply();
        return glowTex;
    }
}

/// <summary>Billboards an additive glow quad toward the camera each frame,
/// pushed just in front of the model it belongs to.</summary>
public class TierGlow : MonoBehaviour
{
    public Vector3 Center;   // local offset of the model centre
    public float Push;       // distance toward the camera, so the flare sits in front of its model

    void LateUpdate() { FaceNow(); }

    // Also runs during manual Camera.Render(), so the editor previews match.
    void OnWillRenderObject() { FaceNow(); }

    public void FaceNow()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        transform.rotation = cam.transform.rotation;
        Vector3 center = transform.parent != null ? transform.parent.TransformPoint(Center) : transform.position;
        transform.position = center - cam.transform.forward * Push;
    }
}

/// <summary>Builds a mob's visual (model + health bar) without movement/AI.</summary>
public static class MobVisual
{
    public const float BarWidth = 0.9f;
    /// <summary>Gap between the top of the mob's model and its floating bar.</summary>
    public const float BarClearance = 0.40f;

    /// <summary>Returns the health-bar root; <paramref name="hpFill"/> is the green fill.
    /// Bosses return null for both: they use the big on-screen HUD bar instead.</summary>
    public static Transform Build(Transform parent, MobDef def, out Transform hpFill)
    {
        GameObject prefab = SnackModels.Load(MobCatalog.ModelPath(def));

        if (prefab != null)
        {
            GameObject m = Object.Instantiate(prefab, parent);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale = Vector3.one * (def.scale * 1.5f);
            SnackModels.CenterOn(m, parent.position);
        }
        else
        {
            ChildModel model = parent.gameObject.AddComponent<ChildModel>();
            Color pants = new Color(def.color.r * 0.45f, def.color.g * 0.45f, def.color.b * 0.55f);
            model.Build(def.color, pants, new Color(0.95f, 0.78f, 0.62f), new Color(0.25f, 0.15f, 0.09f), def.scale, def.longHair);
        }

        // Bosses use the on-screen HUD bar instead of a floating one.
        if (def.archetype == MobArchetype.Boss)
        {
            hpFill = null;
            return null;
        }

        // Sit the bar just clear of the tallest point of the model, so it stays
        // above the head no matter how tall the mob is.
        float barY = TopOf(parent) + BarClearance;

        GameObject hp = new GameObject("HPBar");
        hp.transform.SetParent(parent, false);
        hp.transform.localPosition = new Vector3(0f, barY, 0f);

        TDVisuals.Box(hp.transform, "Bg", Vector3.zero, new Vector3(BarWidth, 0.16f, 0.06f),
            TDVisuals.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.2f));
        GameObject fill = TDVisuals.Box(hp.transform, "Fill", new Vector3(0f, 0f, 0.035f),
            new Vector3(BarWidth, 0.115f, 0.06f), TDVisuals.Mat(new Color(0.28f, 0.90f, 0.30f), 0f, 0.3f));

        hpFill = fill.transform;
        return hp.transform;
    }

    /// <summary>Highest point of the mob's model, measured from the mob's root.</summary>
    static float TopOf(Transform parent)
    {
        Renderer[] rs = parent.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return 1.1f;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return Mathf.Max(0.6f, b.max.y - parent.position.y);
    }

    public static void SetFill(Transform hpFill, float fraction)
    {
        if (hpFill == null) return;
        float f = Mathf.Clamp01(fraction);
        hpFill.localScale = new Vector3(BarWidth * f, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3(-(BarWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }
}
