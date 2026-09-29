using System;
using System.IO;
using System.Text;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod MobWalkCheck.Verify -quit
// Imports the rigged mob GLBs the way the game does (MobVisual.Build) and checks the baked walk
// clip: it must exist and loop, drive the legs, keep the feet at or above the ground, and not
// shift the mob sideways. Writes %TEMP%/opencode/mob_walk_check.txt. Playback speed follows the
// mob's movement in MobWalkAnimation.Update, which only runs in play mode, so that part needs a
// human play-test.
public static class MobWalkCheck
{
    static readonly string[] Mobs = { "Apple", "Carrot", "Pear", "Banana", "Watermelon", "Cherry",
        "Potato", "Orange", "Grapes", "Pumpkin", "Corn", "Tomato" };

    public static void Verify()
    {
        StringBuilder report = new StringBuilder();
        foreach (string id in Mobs)
        foreach (float stageY in new[] { 0f, -400f })
            Check(id, stageY, report);

        string dir = Path.Combine(Path.GetTempPath(), "opencode");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "mob_walk_check.txt");
        File.WriteAllText(file, report.ToString());
        Debug.Log("MobWalkCheck OK\n" + report + "\nWrote " + file);
    }

    static void Check(string id, float stageY, StringBuilder report)
    {
        MobDef def = MobCatalog.Get(id);
        if (def == null) throw new Exception("No MobDef: " + id);
        if (SnackModels.Load(MobCatalog.ModelPath(def)) == null) throw new Exception("Model not loadable: " + id);

        GameObject root = new GameObject("Verify_" + id);
        root.transform.position = new Vector3(4f, stageY, 3f);
        Transform hpFill;
        MobVisual.Build(root.transform, def, out hpFill);

        Animation anim = root.GetComponentInChildren<Animation>();
        if (anim == null) throw new Exception(id + ": no Animation component (importer not set to Legacy?)");
        AnimationState state = null;
        foreach (AnimationState s in anim) { state = s; break; }
        if (state == null || state.clip == null) throw new Exception(id + ": no clip");
        if (!state.clip.legacy) throw new Exception(id + ": clip is not legacy");
        if (state.wrapMode != WrapMode.Loop) throw new Exception(id + ": clip does not loop");
        if (root.GetComponentInChildren<MobWalkAnimation>() == null) throw new Exception(id + ": MobWalkAnimation not attached");

        SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skins.Length == 0) throw new Exception(id + ": no SkinnedMeshRenderer");
        Transform legA = Find(root.transform, "leg_a"), legB = Find(root.transform, "leg_b");
        if (legA == null || legB == null) throw new Exception(id + ": leg bones missing");

        state.enabled = true;
        state.weight = 1f;
        state.speed = 0f;
        float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
        float legAMax = 0f, legBMax = 0f, aBack = 0f, bBack = 0f;
        const int N = 12;
        for (int i = 0; i < N; i++)
        {
            state.time = state.clip.length * i / N;
            anim.Sample();
            foreach (SkinnedMeshRenderer sm in skins)
            {
                Mesh baked = new Mesh();
                sm.BakeMesh(baked, true);
                Transform t = sm.transform;
                foreach (Vector3 v in baked.vertices)
                {
                    Vector3 w = t.position + t.rotation * v;
                    minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y);
                    minX = Mathf.Min(minX, w.x); maxX = Mathf.Max(maxX, w.x);
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            float a = Quaternion.Angle(Quaternion.identity, legA.localRotation);
            float b = Quaternion.Angle(Quaternion.identity, legB.localRotation);
            legAMax = Mathf.Max(legAMax, a); legBMax = Mathf.Max(legBMax, b);
            // opposite legs: when one is swung back the other is swung forward
            float sa = Mathf.Sign(legA.localRotation.x), sb = Mathf.Sign(legB.localRotation.x);
            if (a > 5f && b > 5f && sa == sb) throw new Exception(id + ": legs swing the same way at t=" + state.time);
            aBack += sa; bBack += sb;
        }

        if (legAMax < 8f || legBMax < 8f) throw new Exception(id + ": legs barely move (" + legAMax + "/" + legBMax + " deg)");
        if (minY < stageY - 0.02f) throw new Exception(id + ": feet sink " + (stageY - minY) + " below ground");
        float cx = (minX + maxX) * 0.5f;
        if (Mathf.Abs(cx - 4f) > 0.12f) throw new Exception(id + ": mob drifts off centre (cx=" + cx + ")");

        state.time = 0f;
        anim.Sample();
        report.AppendLine(string.Format(
            "{0,-10} y={1,5:0}  clip={2} len={3:0.00}s  legSwing={4:0.0}/{5:0.0}deg  minY={6:0.000} (ground {7:0}) maxY={8:0.000}  x=[{9:0.00},{10:0.00}]  skins={11}",
            id, stageY, state.clip.name, state.clip.length, legAMax, legBMax, minY, stageY, maxY, minX, maxX, skins.Length));
        UnityEngine.Object.DestroyImmediate(root);
    }

    static Transform Find(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
