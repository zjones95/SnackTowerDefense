using UnityEngine;

// Shared constants for the multiplayer layer.
public static class NetConfig
{
    /// <summary>Maximum players in a lobby, host included.</summary>
    public const int MaxPlayers = 8;

    /// <summary>Default UDP port used for direct/LAN connections.</summary>
    public const ushort DefaultPort = 7777;

    /// <summary>Bumped when the wire protocol changes; mismatched builds are
    /// refused before the match starts. Builds append "+&lt;git sha&gt;" via
    /// Assets/Editor/BuildVersion.cs (see <see cref="FullVersion"/>).
    /// 0.10.0: per-player board theme added to the lobby roster + Hello payload.</summary>
    public const string GameVersion = "0.10.0";

    /// <summary>Relay join codes are 6 characters; direct targets look like "1.2.3.4:7777".</summary>
    public const int RelayCodeLength = 6;

    static string commit;
    static bool commitLoaded;

    /// <summary>The short commit this build was made from, read from
    /// Resources/BuildInfo.txt. Empty when the build predates versioning or the
    /// file is missing (e.g. running from a checkout without a build).</summary>
    public static string Commit()
    {
        if (commitLoaded) return commit;
        commitLoaded = true;
        commit = "";
        TextAsset asset = Resources.Load<TextAsset>("BuildInfo");
        if (asset != null && !string.IsNullOrEmpty(asset.text))
            commit = asset.text.Trim();
        return commit;
    }

    /// <summary>Human-readable version: "0.5.0+abc1234" (or just "0.5.0" when
    /// the commit is unknown).</summary>
    public static string FullVersion
    {
        get
        {
            string c = Commit();
            return string.IsNullOrEmpty(c) ? GameVersion : GameVersion + "+" + c;
        }
    }
}
