using Assets.Scripts.Input;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Room : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _leftEntrance;

    [SerializeField]
    private NavNode2D _rightEntrance;

    [SerializeField]
    private GameObject _waypointsContainer;

    [SerializeField]
    private GameObject _spotsContainer;

    public NavNode2D GetExit(RoomMoveDirection direction)
    {
        return direction switch
        {
            RoomMoveDirection.Left => _leftEntrance,
            RoomMoveDirection.Right => _rightEntrance,
            _ => throw new UnityException($"RoomMoveDirection {direction} is not supported by {nameof(GetExit)} method.")
        };
    }

    public NavNode2D GetEntrance(RoomMoveDirection comeFromDirection)
    {
        return comeFromDirection switch
        {
            RoomMoveDirection.Left => _rightEntrance,
            RoomMoveDirection.Right => _leftEntrance,
            _ => throw new UnityException($"RoomMoveDirection {comeFromDirection} is not supported by {nameof(GetEntrance)} method.")
        };
    }

    public NavNode2D GetWaypoint(int index)
    {
        return _waypointsContainer.transform.GetChild(index).gameObject.GetComponent<NavNode2D>();
    }

    public Spot GetSpot(int index)
    {
        return _spotsContainer.transform.GetChild(index).gameObject.GetComponent<Spot>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (_leftEntrance != null)
        {
            Gizmos.DrawSphere(_leftEntrance.transform.position, 0.1f);
        }

        if (_rightEntrance != null)
        {
            Gizmos.DrawSphere(_rightEntrance.transform.position, 0.1f);
        }
    }
}
