using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Minimal build entry point. CI runs this to prove the project compiles and
// links into a player.
//
// The game bootstraps itself with [RuntimeInitializeOnLoadMethod], so it needs
// no authored scene -- but a Unity build still requires at least one scene, so
// this creates an empty "Boot" scene and registers it if it's missing.
public static class CICompileCheck
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/Boot.unity";

    public static void EnsureBootScene()
    {
        if (!Directory.Exists(SceneDir))
        {
            Directory.CreateDirectory(SceneDir);
            AssetDatabase.Refresh();
        }

        if (!File.Exists(ScenePath))
        {
            // Batchmode starts on an unsaved "Untitled" scene, so additive
            // creation is refused -- Single replaces it, which is safe headless.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("CICompileCheck: created " + ScenePath);
        }

        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath))
        {
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("CICompileCheck: registered " + ScenePath + " in build settings");
        }
    }

    public static void Build()
    {
        EnsureBootScene();

        BuildTarget bt = EditorUserBuildSettings.activeBuildTarget;
        string env = Environment.GetEnvironmentVariable("CI_BUILD_TARGET");
        if (!string.IsNullOrEmpty(env)) bt = (BuildTarget)Enum.Parse(typeof(BuildTarget), env);

        string ext = (bt == BuildTarget.StandaloneWindows64 || bt == BuildTarget.StandaloneWindows) ? ".exe" : "";
        string path = "build/" + bt + "/SnackTowerDefense" + ext;

        string[] scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = bt,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("CICompileCheck: BUILD OK (" + bt + ", " + report.summary.totalSize + " bytes)");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("CICompileCheck: BUILD FAILED (" + report.summary.result + ")");
            EditorApplication.Exit(1);
        }
    }
}
