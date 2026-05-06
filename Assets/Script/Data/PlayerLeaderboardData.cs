using Unity.Netcode;
using System;

public struct PlayerLeaderboardData : INetworkSerializable, IEquatable<PlayerLeaderboardData>
{
    public ulong clientId;
    public int coin;
    public int hp;
    public int tileIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref coin);
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref tileIndex);
    }

    public bool Equals(PlayerLeaderboardData other)
    {
        return clientId == other.clientId &&
               coin == other.coin &&
               hp == other.hp &&
               tileIndex == other.tileIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(clientId, coin, hp, tileIndex);
    }
}