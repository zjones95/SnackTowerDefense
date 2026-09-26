using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Standalone WebGL build for a shareable playable preview:
//   Unity.exe -batchmode -projectPath <proj> -executeMethod WebGLBuild.Build -quit -logFile <log>
// Output goes to build/WebGL/. It is configured for simple static hosting (gzip
// with a JS decompression fallback, so no server Content-Encoding rules needed).
// Note: browser builds are single-player only (Unity Netcode needs UDP sockets).
public static class WebGLBuild
{
    public static void Build()
    {
        CICompileCheck.EnsureBootScene();

        // Static-host friendly: gzip + a JS fallback so it works on any host.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        // BuildPlayer needs the target to be active.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        string path = "build/WebGL";
        string[] scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("WebGLBuild: BUILD OK (" + report.summary.totalSize + " bytes) -> " + path);
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("WebGLBuild: BUILD FAILED (" + report.summary.result + ")");
            EditorApplication.Exit(1);
        }
    }
}
