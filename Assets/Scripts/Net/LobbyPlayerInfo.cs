using UnityEngine;

/// <summary>One player in the lobby. Plain data; exchanged via named messages.</summary>
public struct LobbyPlayerInfo
{
    public ulong ClientId;
    public string Name;
    public bool IsHost;
    /// <summary>Index into <see cref="TDBoardBuilder.PickerThemes"/>: the board
    /// this player picked in the lobby. Host-authoritative; travels in the roster
    /// so everyone can see it and remote boards render the right theme.</summary>
    public int Theme;
    /// <summary>Stable per-install identity (memory/roster only, never on the
    /// wire). Lets a mid-match rejoin adopt its kept seat.</summary>
    public string Guid;
}
