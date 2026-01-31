using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterNavigationController : MonoBehaviour
{
    [SerializeField]
    private NavNode2D _currentNavNode;

    [SerializeField]
    private FaceDirection _currentFaceDirection;

    [SerializeField]
    private float _speed = 2.5f;

    [SerializeField]
    private string _walkStateName = "Walk";

    public bool IsMoving { get; private set; }

    private Coroutine _co;
    private Animator _animator;
    private Vector3? _originalScale;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void GoTo(Spot spot)
    {
        if (IsMoving)
        {
            return;
        }

        NavNode2D targetNode = spot.GetApproachNode();
        List<NavNode2D> path = GraphPathfinder2D.FindPath(_currentNavNode, targetNode);

        if (path == null || path.Count == 0)
        {
            return;
        }

        if (_co != null)
        {
            StopCoroutine(_co);
        }
        _co = StartCoroutine(MoveCo(path, spot.GetFaceDirection()));
    }

    private IEnumerator MoveCo(List<NavNode2D> nodes, FaceDirection targetFaceDirection)
    {
        IsMoving = true;

        _originalScale ??= transform.localScale;

        _animator.Play(_walkStateName);

        foreach (var node in nodes)
        {
            FaceDirection desiredFaceDirection = node.transform.position.x > _currentNavNode.transform.position.x ? 
                FaceDirection.Right : FaceDirection.Left;

            UpdateFaceDirection(desiredFaceDirection);

            _currentNavNode = node;
            Vector3 desiredScale = _originalScale.Value * node.GetDesiredScale();

            yield return MoveTo((Vector2)node.transform.position, desiredScale);
        }

        UpdateFaceDirection(targetFaceDirection);

        _animator.Play("Idle");

        IsMoving = false;
        _co = null;
    }

    private void UpdateFaceDirection(FaceDirection desiredFaceDirection)
    {
        if (_currentFaceDirection != desiredFaceDirection)
        {
            _currentFaceDirection = desiredFaceDirection;
            gameObject.transform.Rotate(0, 180, 0);
        }
    }

    private IEnumerator MoveTo(Vector2 target, Vector3 targetScale)
    {
        float totalDist = ((Vector2)transform.position - target).magnitude;
        float currentDist = totalDist;

        Vector3 startingScale = transform.localScale;
        Vector3 dScale = targetScale - startingScale;

        while (currentDist > 0.0025f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, _speed * Time.deltaTime);
            currentDist = ((Vector2)transform.position - target).magnitude;

            transform.localScale = startingScale + dScale * (totalDist - currentDist) / totalDist;

            yield return null;
        }
    }
}
