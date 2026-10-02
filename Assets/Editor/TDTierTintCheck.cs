using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod TDTierTintCheck.Verify -quit
// Builds every tower visual the way the game does (TowerVisual.Build) and reports
// whether the tier tint landed on the base ring: renderer names, material names,
// shaders and resulting colours vs the expected tier colour. Writes
// %TEMP%/opencode/tier_tint_check.txt. Appearance still needs a human play-test.
public static class TDTierTintCheck
{
    public static void Verify()
    {
        StringBuilder report = new StringBuilder();
        DumpRimShader(report);
        int problems = 0;
        foreach (TowerType t in TowerCatalog.GalleryTypes)
        {
            int tier = TowerCatalog.IsT7Type(t) ? TowerCatalog.MaxTier : 3; // T3 green is unmistakable
            GameObject root = new GameObject("Tint_" + t);
            try
            {
                TowerVisual.Build(root.transform, t, tier);
                Color want = TowerVisual.TierColour(tier);
                Renderer[] rs = root.GetComponentsInChildren<Renderer>();
                report.AppendLine(t + " renderers=" + rs.Length);
                bool rimFound = false, tintOk = false;
                foreach (Renderer r in rs)
                {
                    string matName = r.sharedMaterial != null ? r.sharedMaterial.name : "<none>";
                    string shader = r.sharedMaterial != null && r.sharedMaterial.shader != null
                        ? r.sharedMaterial.shader.name : "<none>";
                    Color got = ReadTint(r.sharedMaterial);
                    bool match = ColorsClose(got, want);
                    string flags = "";
                    if (r.name.ToLower() == "rim") { flags += "[rim]"; rimFound = true; }
                    if (matName.Contains("TierOrange")) flags += "[tiermat]";
                    if (match) tintOk = true;
                    report.AppendLine("  obj=" + r.name + " mat=" + matName +
                        " shader=" + shader + " color=" + got + flags);
                }
                report.AppendLine("  want=" + want + " rimFound=" + rimFound + " tintOk=" + tintOk);
                if (!rimFound) { report.AppendLine("  PROBLEM: no rim object"); problems++; }
                else if (!tintOk) { report.AppendLine("  PROBLEM: rim not tier-tinted"); problems++; }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        string dir = Path.Combine(Path.GetTempPath(), "opencode");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "tier_tint_check.txt");
        File.WriteAllText(file, report.ToString());
        Debug.Log("TDTierTintCheck problems=" + problems + "\n" + report + "\nWrote " + file);
        if (problems > 0) throw new Exception("TDTierTintCheck: " + problems + " problems (see " + file + ")");
    }

    static void DumpRimShader(StringBuilder report)
    {
        GameObject root = new GameObject("TintDump");
        try
        {
            TowerVisual.Build(root.transform, TowerType.SingleShot, 3);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (r.name.ToLower() != "rim" || r.sharedMaterial == null) continue;
                Shader sh = r.sharedMaterial.shader;
                report.AppendLine("rim shader=" + (sh != null ? sh.name : "<none>"));
                if (sh == null) return;
                int n = ShaderUtil.GetPropertyCount(sh);
                for (int i = 0; i < n; i++)
                {
                    report.AppendLine("  prop=" + ShaderUtil.GetPropertyName(sh, i) +
                        " type=" + ShaderUtil.GetPropertyType(sh, i));
                }
                return;
            }
            report.AppendLine("rim object not found for dump");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static Color ReadTint(Material m)
    {
        if (m == null) return new Color(-1f, -1f, -1f);
        if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        return new Color(-2f, -2f, -2f);
    }

    static bool ColorsClose(Color a, Color b)
    {
        return Math.Abs(a.r - b.r) < 0.05f && Math.Abs(a.g - b.g) < 0.05f && Math.Abs(a.b - b.b) < 0.05f;
    }
}
