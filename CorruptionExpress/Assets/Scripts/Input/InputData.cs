using System;
using Unity.Netcode;

namespace Assets.Scripts.Input
{
    public struct InputData : INetworkSerializable, IEquatable<InputData>
    {
        //Planning input
        public ActionType ActionType;

        //Execution input
        public SpotInput SpotInput; //For Search or Put
        public long TargetClientId; //For Search or Give to Player
        public RoomMoveDirection MoveDirection; //For Move

        public static InputData FromAction(ActionType action)
        {
            return new InputData()
            {
                ActionType = action,
                MoveDirection = RoomMoveDirection.None,
                SpotInput = SpotInput.Empty,
                TargetClientId = 0
            };
        }

        public static InputData FromMoveDirection(RoomMoveDirection direction)
        {
            return new InputData()
            {
                ActionType = ActionType.None,
                MoveDirection = direction,
                SpotInput = SpotInput.Empty,
                TargetClientId = 0
            };
        }

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
        public bool HasTargetPlayerInput() => TargetClientId != -1;
        public bool HasMoveDirectionInput() => MoveDirection != RoomMoveDirection.None;
        public bool HasActionTypeInput() => ActionType != ActionType.None;
    }
}
