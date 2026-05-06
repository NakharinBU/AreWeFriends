using Unity.Netcode;
using System;

[System.Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong clientId;
    public TeamType team;
    public bool isReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref team);
        serializer.SerializeValue(ref isReady);
    }

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId &&
               team == other.team &&
               isReady == other.isReady;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(clientId, team, isReady);
    }
}