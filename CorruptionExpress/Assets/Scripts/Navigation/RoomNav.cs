using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _leftEntrance;

    [SerializeField]
    private NavNode2D _rightEntrance;

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
