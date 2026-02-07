using UnityEngine;

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
