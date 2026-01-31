using UnityEngine;

public class Spot : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _approachNode;

    [SerializeField]
    private FaceDirection _faceDirection;

    public NavNode2D GetApproachNode() => _approachNode;
    public FaceDirection GetFaceDirection() => _faceDirection;

    private void OnDrawGizmos()
    {
        if (_approachNode != null)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(transform.position, _approachNode.transform.position);
        }
    }
}