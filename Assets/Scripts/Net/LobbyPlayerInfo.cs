using UnityEngine;

/// <summary>One player in the lobby. Plain data; exchanged via named messages.</summary>
public struct LobbyPlayerInfo
{
    public ulong ClientId;
    public string Name;
    public bool IsHost;
}
