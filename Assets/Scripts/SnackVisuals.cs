using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the *visual* of a tower (model, rotating head, tier label) without any
/// gameplay logic, so both live towers and remote/spectated copies share one look.
/// </summary>
public static class TowerVisual
{
    /// <summary>Visual size per tier. Tier 1 starts deliberately small and each
    /// step grows the tower, but the top tiers are damped so a ~1-unit model
    /// doesn't clip its neighbours on a 2-unit tile (see docs/TierPlan.md).</summary>
    static readonly float[] TierScales = { 0.78f, 1.00f, 1.22f, 1.34f, 1.44f, 1.54f, 1.66f };

    public static float TierScale(int tier)
    {
        return TierScales[Mathf.Clamp(tier, 1, TierScales.Length) - 1];
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

        return turret;
    }

    /// <summary>Tier palette — the same colours for every tower type. Tiers 4-6
    /// deliberately avoid gold, which is reserved for the Gold tower; tier 7 is
    /// the fusion gold used to mark the unique T6+T6 result.</summary>
    public static readonly Color[] TierColours =
    {
        new Color(0.80f, 0.82f, 0.86f),  // 1 - grey
        new Color(0.30f, 0.60f, 1.00f),  // 2 - blue
        new Color(0.30f, 0.92f, 0.42f),  // 3 - green
        new Color(0.85f, 0.45f, 0.25f),  // 4 - bronze
        new Color(0.95f, 0.30f, 0.85f),  // 5 - magenta
        new Color(0.25f, 0.95f, 1.00f),  // 6 - cyan
        new Color(1.00f, 0.92f, 0.55f)   // 7 - fusion gold
    };

    public static Color TierColour(int tier)
    {
        return TierColours[Mathf.Clamp(tier, 1, TierColours.Length) - 1];
    }

    /// <summary>Thin dark outline drawn behind the digit, in world units.</summary>
    const float OutlineOffset = 0.008f;
    /// <summary>Both the outline copies and the coloured digit are drawn at half opacity.</summary>
    const float LabelAlpha = 0.5f;
    static readonly Color OutlineColour = new Color(0.07f, 0.06f, 0.09f, LabelAlpha);

    /// <summary>Floating tier number above the tower, tinted with the tier colour
    /// and outlined in dark so it stays readable without a background plate.</summary>
    public static Transform BuildTierLabel(Transform parent, int tier)
    {
        GameObject tierGO = new GameObject("TierLabel");
        tierGO.transform.SetParent(parent, false);
        tierGO.transform.localPosition = new Vector3(0f, LabelHeight(parent), 0f);
        tierGO.AddComponent<BillboardLabel>();

        // a ring of offset copies behind the coloured digit makes a thin outline
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            AddTierText(tierGO.transform, tier,
                new Vector3(Mathf.Cos(a) * OutlineOffset, Mathf.Sin(a) * OutlineOffset, 0.004f),
                OutlineColour, 0);
        }
        AddTierText(tierGO.transform, tier, Vector3.zero, TierColour(tier), 1);

        return tierGO.transform;
    }

    static void AddTierText(Transform parent, int tier, Vector3 offset, Color colour, int order)
    {
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(parent, false);
        txtGO.transform.localPosition = offset;
        TextMesh tm = txtGO.AddComponent<TextMesh>();
        tm.text = tier.ToString();
        tm.characterSize = 0.042f;
        tm.fontSize = 120;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(colour.r, colour.g, colour.b, LabelAlpha);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer tr = txtGO.GetComponent<MeshRenderer>();
            if (tr != null)
            {
                tr.sharedMaterial = font.material;
                tr.sortingOrder = order;   // outline first, coloured digit on top
            }
        }
    }

    /// <summary>Top of whatever model already sits under parent, plus a small gap.</summary>
    static float LabelHeight(Transform parent)
    {
        Renderer[] rs = parent.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return 1.55f;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return Mathf.Max(1.0f, b.max.y - parent.position.y + 0.42f);
    }
}

/// <summary>Keeps a label facing the camera (TextMesh reads from its -Z face).</summary>
public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}

/// <summary>Builds a mob's visual (model + health bar) without movement/AI.</summary>
public static class MobVisual
{
    public const float BarWidth = 0.9f;
    /// <summary>Gap between the top of the mob's model and its floating bar.</summary>
    public const float BarClearance = 0.40f;

    /// <summary>Returns the health-bar root; <paramref name="hpFill"/> is the green fill.
    /// Bosses normally return null (they use the big on-screen HUD bar), unless
    /// <paramref name="bossBar"/> is set — remote boards pass true so you can also
    /// see other players' bosses.</summary>
    public static Transform Build(Transform parent, MobDef def, out Transform hpFill, bool bossBar = false)
    {
        GameObject prefab = SnackModels.Load(MobCatalog.ModelPath(def));

        if (prefab != null)
        {
            GameObject m = Object.Instantiate(prefab, parent);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale = Vector3.one * (def.scale * 1.5f);
            SnackModels.CenterOn(m, parent.position);
            MobWalkAnimation.Attach(m);
        }
        else
        {
            ChildModel model = parent.gameObject.AddComponent<ChildModel>();
            Color pants = new Color(def.color.r * 0.45f, def.color.g * 0.45f, def.color.b * 0.55f);
            model.Build(def.color, pants, new Color(0.95f, 0.78f, 0.62f), new Color(0.25f, 0.15f, 0.09f), def.scale, def.longHair);
        }

        // Bosses use the on-screen HUD bar instead of a floating one (unless a
        // remote board explicitly wants a bar so their boss is visible too).
        if (def.archetype == MobArchetype.Boss && !bossBar)
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

        GameObject bg = TDVisuals.Box(hp.transform, "Bg", Vector3.zero, new Vector3(BarWidth, 0.16f, 0.06f),
            TDVisuals.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.2f));
        GameObject fill = TDVisuals.Box(hp.transform, "Fill", new Vector3(0f, 0f, 0.035f),
            new Vector3(BarWidth, 0.115f, 0.06f), TDVisuals.Mat(new Color(0.28f, 0.90f, 0.30f), 0f, 0.3f));
        NoShadow(bg);
        NoShadow(fill);

        hpFill = fill.transform;
        BuildStatusIcons(hp.transform);
        return hp.transform;
    }

    static Material burnIconMat, slowIconMat, tarIconMat;
    static Shader iconShader;

    /// <summary>Tiny status icon quads sit at the top-right of the floating bar:
    /// burn (with its stack count) then slow, then tar. All hide until the mob
    /// is actually affected, and billboard with the bar because they are its
    /// children. The bar is billboarded with LookRotation(direction to camera),
    /// so its local +X points screen-LEFT — the icons use negative X to land on
    /// the right.</summary>
    static void BuildStatusIcons(Transform hp)
    {
        if (burnIconMat == null) burnIconMat = IconMaterial(TDTextures.IconBurn());
        if (slowIconMat == null) slowIconMat = IconMaterial(TDTextures.IconSlow());
        if (tarIconMat == null) tarIconMat = IconMaterial(TDTextures.IconTar());

        float burnX = -(BarWidth * 0.5f + 0.16f);
        float slowX = -(BarWidth * 0.5f + 0.38f);
        float tarX = -(BarWidth * 0.5f + 0.60f);

        GameObject burn = TDVisuals.Quad(hp, "IconBurn",
            new Vector3(burnX, 0.10f, 0f), 0.24f, burnIconMat);
        GameObject slow = TDVisuals.Quad(hp, "IconSlow",
            new Vector3(slowX, 0.10f, 0f), 0.24f, slowIconMat);
        GameObject tar = TDVisuals.Quad(hp, "IconTar",
            new Vector3(tarX, 0.10f, 0f), 0.24f, tarIconMat);
        NoShadow(burn);
        NoShadow(slow);
        NoShadow(tar);

        GameObject labelGO = new GameObject("Stacks");
        labelGO.transform.SetParent(hp, false);
        labelGO.transform.localPosition = new Vector3(burnX - 0.14f, 0.10f + 0.15f, 0.005f);
        labelGO.AddComponent<BillboardLabel>();
        TextMesh tm = labelGO.AddComponent<TextMesh>();
        tm.text = "x1";
        tm.characterSize = 0.028f;
        tm.fontSize = 90;
        tm.anchor = TextAnchor.LowerLeft;
        tm.alignment = TextAlignment.Left;
        tm.color = new Color(1f, 0.82f, 0.42f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = labelGO.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }

        MobStatusIcons icons = hp.gameObject.AddComponent<MobStatusIcons>();
        icons.Burn = burn.transform;
        icons.Slow = slow.transform;
        icons.Tar = tar.transform;
        icons.Stacks = tm;
        icons.Set(false, false, 0, false);
    }

    /// <summary>Build-safe alpha material for a status icon (same Resources shader
    /// SplashFX uses, so the transparent variant can't be stripped from a build).</summary>
    static Material IconMaterial(Texture2D tex)
    {
        if (iconShader == null) iconShader = Resources.Load<Shader>("Snack/Fx");
        if (iconShader == null) iconShader = Shader.Find("Snack/Fx");

        Material m = iconShader != null ? new Material(iconShader)
                                        : TDVisuals.TransparentMat(Color.white, 1f, 0.15f);
        m.mainTexture = tex;
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        return m;
    }

    /// <summary>Health bars shouldn't cast or receive shadows.</summary>
    static void NoShadow(GameObject g)
    {
        Renderer r = g.GetComponent<Renderer>();
        if (r == null) return;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
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
        hpFill.localPosition = new Vector3((BarWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }
}

/// <summary>The burn/slow/tar icon quads and stack label hanging off a mob's
/// floating health bar. Lives on the bar root, so fetch it with
/// <see cref="Get"/> or <c>bar.GetComponent&lt;MobStatusIcons&gt;()</c>.</summary>
public class MobStatusIcons : MonoBehaviour
{
    public Transform Burn;
    public Transform Slow;
    public Transform Tar;
    public TextMesh Stacks;

    public static MobStatusIcons Get(Transform barRoot)
    {
        return barRoot != null ? barRoot.GetComponent<MobStatusIcons>() : null;
    }

    /// <summary>Toggles the three icons and updates the burn stack count.
    /// <paramref name="slowed"/> covers both slow and stun.</summary>
    public void Set(bool slowed, bool burning, int stacks, bool tarred)
    {
        Toggle(Slow, slowed);
        Toggle(Burn, burning);
        Toggle(Tar, tarred);

        if (Stacks == null) return;
        bool show = burning && stacks > 0;
        Toggle(Stacks.transform, show);
        if (show)
        {
            string s = "x" + stacks;
            if (Stacks.text != s) Stacks.text = s;
        }
    }

    static void Toggle(Transform t, bool on)
    {
        if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
    }
}
