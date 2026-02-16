using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NavNode2D : MonoBehaviour
{
    [SerializeField]
    private List<NavNode2D> _neighbors = new();

    [SerializeField]
    private float _desiredScale = 1.0f;

    public NavNode2D() { }
    public NavNode2D(float desiredScale)
    {
        _desiredScale = desiredScale;
    }

    public List<NavNode2D> GetNeigbours() => _neighbors;
    public float GetDesiredScale() => _desiredScale;

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, 0.05f);

        foreach (var n in _neighbors)
        {
            if (n != null)
            {
                Gizmos.color = n._neighbors.Contains(this) ? Color.red : Color.cyan;
                Gizmos.DrawLine(transform.position, n.transform.position);
            }
        }
    }
}