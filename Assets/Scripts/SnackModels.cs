using System.Collections.Generic;
using UnityEngine;

// Loads the Blender-authored GLB models from Resources (falls back to the
// procedural art when a model is missing).
public static class SnackModels
{
    private static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    public static GameObject Load(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        GameObject g;
        if (cache.TryGetValue(path, out g)) return g;
        g = Resources.Load<GameObject>(path);
        cache[path] = g;
        return g;
    }

    public static string TowerPath(TowerType t)
    {
        return "Snack/Towers/" + TowerName(t);
    }

    public static string TowerName(TowerType t)
    {
        switch (t)
        {
            case TowerType.SingleShot: return "Popcorn";
            case TowerType.Splash: return "Soda";
            case TowerType.Slow: return "Gum";
            case TowerType.Sniper: return "SourStraw";
            case TowerType.Chain: return "SourBelt";
            case TowerType.Pierce: return "Pierce";
            case TowerType.Bounce: return "Bounce";
            case TowerType.Poison: return "Wasabi";
            default: return "Popcorn";
        }
    }

    public const string CrackerPath = "Snack/Mobs/Cracker";

    // Recenters an instantiated model so it sits on the cell: the pedestal
    // (base) is aligned to the anchor in X/Z with its bottom at the anchor Y.
    public static void CenterOn(GameObject instance, Vector3 worldAnchor)
    {
        Renderer[] rs = instance.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return;

        Renderer anchorRenderer = null;
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i].name.ToLower().Contains("pedestal")) { anchorRenderer = rs[i]; break; }
        }

        Bounds b;
        if (anchorRenderer != null)
        {
            b = anchorRenderer.bounds;
        }
        else
        {
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        }

        instance.transform.position += new Vector3(worldAnchor.x - b.center.x, worldAnchor.y - b.min.y, worldAnchor.z - b.center.z);
    }
}
