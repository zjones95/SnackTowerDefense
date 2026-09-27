using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;   // disambiguate from System.Diagnostics.Debug

// Stamps the build with its git commit before a player is built:
//   * PlayerSettings.bundleVersion becomes "<scheme>+<short sha>"
//   * Assets/Resources/BuildInfo.txt is written so the running game can read
//     the commit back through NetConfig.Commit()/FullVersion.
//
// The git call is a plain process launch (no shell), so a missing repo/git
// simply falls back to a timestamp. Nothing in here is allowed to throw out of
// the build callback -- a version stamp must never fail the build.
public class BuildVersion : IPreprocessBuildWithReport
{
    const string BuildInfoDir = "Assets/Resources";
    const string BuildInfoPath = BuildInfoDir + "/BuildInfo.txt";

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            string commit = GitCommit();
            bool fromGit = !string.IsNullOrEmpty(commit);
            if (!fromGit)
                commit = "nogit-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            PlayerSettings.bundleVersion = BaseVersion() + "+" + commit;
            WriteBuildInfo(commit);
            Debug.Log("BuildVersion: bundleVersion=" + PlayerSettings.bundleVersion +
                      (fromGit ? "" : " (git unavailable)"));
        }
        catch (Exception e)
        {
            // Never let version stamping fail the build.
            Debug.LogWarning("BuildVersion: could not stamp the build version: " + e.Message);
        }
    }

    /// <summary>Existing version scheme, with any previous "+build" metadata
    /// removed. Falls back to the networked game version when the project only
    /// has Unity's default "1.0", so builds track the protocol version.</summary>
    static string BaseVersion()
    {
        string current = PlayerSettings.bundleVersion ?? "";
        int plus = current.IndexOf('+');
        string prefix = (plus >= 0 ? current.Substring(0, plus) : current).Trim();
        if (string.IsNullOrEmpty(prefix) || prefix == "1.0") prefix = NetConfig.GameVersion;
        return prefix;
    }

    static void WriteBuildInfo(string commit)
    {
        try
        {
            if (!Directory.Exists(BuildInfoDir)) Directory.CreateDirectory(BuildInfoDir);
            File.WriteAllText(BuildInfoPath, commit + Environment.NewLine);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogWarning("BuildVersion: could not write " + BuildInfoPath + ": " + e.Message);
        }
    }

    /// <summary>Short HEAD sha via `git rev-parse --short HEAD`, run in the
    /// project root. Returns "" when git or the repository is missing.</summary>
    static string GitCommit()
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var p = Process.Start(psi))
            {
                if (p == null) return "";
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                if (!p.HasExited)
                {
                    try { p.Kill(); } catch { /* ignore */ }
                    return "";
                }
                if (p.ExitCode != 0) return "";
                return output.Trim();
            }
        }
        catch
        {
            return "";   // git not installed / not a repo / blocked
        }
    }
}
