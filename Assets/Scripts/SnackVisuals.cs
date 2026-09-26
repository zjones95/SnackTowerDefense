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

    /// <summary>Coloured glow marking the tower's tier: a bright ring on the mat
    /// plus a fainter outer ring. Flat, opaque geometry — it can never cover the
    /// model and never depends on transparency (which the player build strips).
    /// Same palette for every tower type.</summary>
    static void BuildTierGlow(Transform parent, int tier)
    {
        Renderer[] rs = parent.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float half = Mathf.Max(0.22f, Mathf.Max(b.extents.x, b.extents.z));

        Color c = TierColour(tier);
        Color dim = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f);
        Ring(parent, "TierRing", half + 0.12f, 0.055f, 0.11f, TDVisuals.EmissiveMat(c, 1.1f));
        Ring(parent, "TierHalo", half + 0.34f, 0.035f, 0.07f, TDVisuals.EmissiveMat(dim, 0.5f));
    }

    /// <summary>A thin flat ring of small blocks laid on the mat at the given radius.</summary>
    static void Ring(Transform parent, string name, float radius, float height, float thickness, Material m)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);

        int segs = Mathf.Clamp(Mathf.RoundToInt(radius * 44f), 16, 56);
        float tang = (2f * Mathf.PI * radius / segs) * 1.25f;
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            GameObject g = TDVisuals.Box(ring.transform, "s" + i,
                new Vector3(Mathf.Cos(a) * radius, height * 0.5f + 0.006f, Mathf.Sin(a) * radius),
                new Vector3(tang, height, thickness), m);
            g.transform.localRotation = Quaternion.Euler(0f, 90f - a * Mathf.Rad2Deg, 0f);
        }
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
