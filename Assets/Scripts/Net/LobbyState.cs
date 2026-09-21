using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>One entry in the lobby player list. Serializable over the network.</summary>
public struct LobbyPlayerInfo : INetworkSerializable, IEquatable<LobbyPlayerInfo>
{
    public ulong ClientId;
    public FixedString64Bytes Name;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
    }

    public bool Equals(LobbyPlayerInfo other) => ClientId == other.ClientId;
    public override bool Equals(object obj) => obj is LobbyPlayerInfo o && Equals(o);
    public override int GetHashCode() => ClientId.GetHashCode();
}

/// <summary>
/// Server-owned lobby state. One instance is spawned by the host after StartHost();
/// it replicates to every client automatically.
/// </summary>
public class LobbyState : NetworkBehaviour
{
    public readonly NetworkList<LobbyPlayerInfo> Players = new NetworkList<LobbyPlayerInfo>();
    public readonly NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (NetworkSession.Instance != null) NetworkSession.Instance.SetLobby(this);
        if (IsServer) MatchStarted.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkSession.Instance != null) NetworkSession.Instance.ClearLobby(this);
    }

    /// <summary>Server -> all clients: the host pressed Start.</summary>
    [ClientRpc]
    public void BeginMatchClientRpc()
    {
        NetworkSession.NotifyMatchStarted();
    }
}
