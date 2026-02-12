using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpotNetState))]
public class Spot : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _approachNode;

    [SerializeField]
    private FaceDirection _faceDirection;

    public NavNode2D GetApproachNode() => _approachNode;
    public FaceDirection GetFaceDirection() => _faceDirection;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PutItem()
    {
        GetComponent<SpotNetState>().ItemsCount.Value++;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public bool TakeItem()
    {
        SpotNetState state = GetComponent<SpotNetState>();
        if (state.ItemsCount.Value > 0)
        {
            state.ItemsCount.Value--;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (_approachNode != null)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(transform.position, _approachNode.transform.position);
        }

        if (GetComponent<SpotNetState>().ItemsCount.Value > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}