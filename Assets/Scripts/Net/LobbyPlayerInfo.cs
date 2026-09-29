using UnityEngine;

/// <summary>One player in the lobby. Plain data; exchanged via named messages.</summary>
public struct LobbyPlayerInfo
{
    public ulong ClientId;
    public string Name;
    public bool IsHost;
    /// <summary>Stable per-install identity (memory/roster only, never on the
    /// wire). Lets a mid-match rejoin adopt its kept seat.</summary>
    public string Guid;
    /// <summary>Board skin choice (on the wire). Each player's board renders
    /// in their own theme.</summary>
    public int Theme;
}
