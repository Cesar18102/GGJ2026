using System;
using Unity.Netcode;

namespace Assets.Scripts.Input
{
    public struct SpotInput : INetworkSerializable, IEquatable<SpotInput>
    {
        public static SpotInput Empty = new SpotInput(-1, -1);

        public int RoomId;
        public int SpotId;

        public SpotInput(int roomId, int spotId)
        {
            RoomId = roomId;
            SpotId = spotId;
        }

        public bool Equals(SpotInput other)
        {
            return RoomId == other.RoomId && SpotId == other.SpotId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoomId);
            serializer.SerializeValue(ref SpotId);
        }
    }
}
