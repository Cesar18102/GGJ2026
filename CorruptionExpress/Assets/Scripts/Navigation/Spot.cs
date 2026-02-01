using UnityEngine;

public class Spot : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _approachNode;

    [SerializeField]
    private FaceDirection _faceDirection;

    public NavNode2D GetApproachNode() => _approachNode;
    public FaceDirection GetFaceDirection() => _faceDirection;

    private bool _hasItem;

    public void PutItem()
    {
        _hasItem = true;
    }

    public void TakeItem()
    {
        _hasItem = false;
    }

    private void OnDrawGizmos()
    {
        if (_approachNode != null)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(transform.position, _approachNode.transform.position);
        }

        if (_hasItem)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}