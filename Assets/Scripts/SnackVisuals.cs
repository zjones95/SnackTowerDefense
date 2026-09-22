using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the *visual* of a tower (model, rotating head, tier badge) without any
/// gameplay logic, so both live towers and remote/spectated copies share one look.
/// </summary>
public static class TowerVisual
{
    /// <summary>Creates the tower model and returns the rotating head pivot.</summary>
    public static Transform Build(Transform parent, TowerType type, int tier)
    {
        Transform turret;

        GameObject prefab = SnackModels.Load(SnackModels.TowerPath(type));
        if (prefab != null)
        {
            GameObject model = Object.Instantiate(prefab, parent);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            SnackModels.CenterOn(model, parent.position);

            Transform pivot = new GameObject("HeadPivot").transform;
            pivot.SetParent(parent, false);
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
            tg.transform.SetParent(parent, false);
            tg.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            turret = tg.transform;
            SnackArt.BuildTower(parent, turret, type, tier);
        }

        return turret;
    }

    /// <summary>Tier number badge above the tower; billboard it toward the camera.</summary>
    public static Transform BuildTierLabel(Transform parent, int tier)
    {
        GameObject tierGO = new GameObject("TierLabel");
        tierGO.transform.SetParent(parent, false);
        tierGO.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        tierGO.AddComponent<BillboardLabel>();

        TDVisuals.Box(tierGO.transform, "Badge", new Vector3(0f, 0f, 0.02f), new Vector3(0.40f, 0.52f, 0.02f),
            TDVisuals.TransparentMat(new Color(0.08f, 0.08f, 0.10f), 0.5f, 0.3f));

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(tierGO.transform, false);
        txtGO.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        TextMesh tm = txtGO.AddComponent<TextMesh>();
        tm.text = tier.ToString();
        tm.characterSize = 0.055f;
        tm.fontSize = 110;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.96f, 0.70f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer tr = txtGO.GetComponent<MeshRenderer>();
            if (tr != null) tr.sharedMaterial = font.material;
        }

        return tierGO.transform;
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

    /// <summary>Returns the health-bar root; <paramref name="hpFill"/> is the green fill.</summary>
    public static Transform Build(Transform parent, MobDef def, out Transform hpFill)
    {
        GameObject prefab = def.type == MobType.Basic ? SnackModels.Load(SnackModels.CrackerPath) : null;
        float barY;

        if (prefab != null)
        {
            GameObject m = Object.Instantiate(prefab, parent);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale = Vector3.one * (def.scale * 1.5f);
            SnackModels.CenterOn(m, parent.position);
            barY = 0.9f;
        }
        else
        {
            ChildModel model = parent.gameObject.AddComponent<ChildModel>();
            Color pants = new Color(def.color.r * 0.45f, def.color.g * 0.45f, def.color.b * 0.55f);
            model.Build(def.color, pants, new Color(0.95f, 0.78f, 0.62f), new Color(0.25f, 0.15f, 0.09f), def.scale);
            barY = def.scale * 1.6f + 0.3f;
        }

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

    public static void SetFill(Transform hpFill, float fraction)
    {
        if (hpFill == null) return;
        float f = Mathf.Clamp01(fraction);
        hpFill.localScale = new Vector3(BarWidth * f, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3(-(BarWidth * (1f - f)) * 0.5f, hpFill.localPosition.y, hpFill.localPosition.z);
    }
}
