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

    public enum RoomMoveDirection
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    public struct InputData : INetworkSerializable, IEquatable<InputData>
    {
        //Planning input
        public ActionType ActionType;

        //Execution input
        public SpotInput SpotInput; //For Search or Put
        public ulong TargetClientId; //For Search or Give to Player
        public RoomMoveDirection MoveDirection; //For Move

        public bool Equals(InputData other)
        {
            return ActionType == other.ActionType &&
                SpotInput.Equals(other.SpotInput) &&
                TargetClientId == other.TargetClientId &&
                MoveDirection == other.MoveDirection;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ActionType);
            serializer.SerializeValue(ref SpotInput);
            serializer.SerializeValue(ref TargetClientId);
            serializer.SerializeValue(ref MoveDirection);
        }

        public bool HasSpotInput() => !SpotInput.Equals(SpotInput.Empty);
        public bool HasTargetPlayerInput() => TargetClientId != 0; //???
        public bool HasMoveDirectionInput() => MoveDirection != RoomMoveDirection.None;
        public bool HasActionTypeInput() => ActionType != ActionType.None;
    }
}
