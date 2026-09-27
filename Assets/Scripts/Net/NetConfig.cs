// Shared constants for the multiplayer layer.
public static class NetConfig
{
    /// <summary>Maximum players in a lobby, host included.</summary>
    public const int MaxPlayers = 8;

    /// <summary>Default UDP port used for direct/LAN connections.</summary>
    public const ushort DefaultPort = 7777;

    /// <summary>Bumped when the protocol changes; mismatched builds are refused.</summary>
    public const string GameVersion = "0.2.0";

    /// <summary>Relay join codes are 6 characters; direct targets look like "1.2.3.4:7777".</summary>
    public const int RelayCodeLength = 6;
}
