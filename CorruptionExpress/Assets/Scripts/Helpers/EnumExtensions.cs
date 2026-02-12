using Assets.Scripts.Input;
using UnityEngine;

public static class EnumExtensions
{
    public static int ToRoomIndexDelta(this RoomMoveDirection direction)
    {
        return direction switch
        {
            RoomMoveDirection.Left => -1,
            RoomMoveDirection.Right => 1,
            _ => throw new UnityException($"RoomMoveDirection {direction} is not supported")
        };
    }

    public static FaceDirection ToFaceDirection(this RoomMoveDirection direction)
    {
        return direction switch
        {
            RoomMoveDirection.Left => FaceDirection.Left,
            RoomMoveDirection.Right => FaceDirection.Right,
            _ => throw new UnityException($"RoomMoveDirection {direction} is not supported")
        };
    }
}
