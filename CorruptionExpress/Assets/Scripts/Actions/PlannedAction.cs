using System;
using Unity.Netcode;

public struct PlannedAction : INetworkSerializable, IEquatable<PlannedAction>
{
    public ulong ClientId;
    public ActionType Action;

    public PlannedAction(ulong clientId, ActionType action)
    {
        ClientId = clientId;
        Action = action;
    }

    public bool Equals(PlannedAction other)
    {
        return ClientId == other.ClientId && Action == other.Action;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Action);
    }
}