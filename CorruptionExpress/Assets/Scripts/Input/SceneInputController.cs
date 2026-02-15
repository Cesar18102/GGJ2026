using Assets.Scripts.Input;
using GameState;
using Teams;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneInputController : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    private Vector2 _lastPointerPos;

    public void OnPoint(InputAction.CallbackContext ctx)
    {
        _lastPointerPos = ctx.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        Vector2 worldPos = _camera.ScreenToWorldPoint(_lastPointerPos);
        Collider2D collider = Physics2D.OverlapPoint(worldPos);
        
        if (collider?.gameObject != null)
        {
            InputData input = GetInputData(collider.gameObject);
            GameStateManager.Instance.HandleInputServerRpc(input);
        }
    }

    private InputData GetInputData(GameObject obj)
    {
        return new InputData()
        {
            ActionType = ActionType.None,
            MoveDirection = RoomMoveDirection.None,
            SpotInput = GetSpotInput(obj),
            TargetClientId = (long?)(obj.GetComponentInParent<PlayerNetState>()?.NetworkObject.OwnerClientId) ?? -1
        };
    }

    private SpotInput GetSpotInput(GameObject obj)
    {
        Spot spot = obj.GetComponent<Spot>();

        if (spot == null)
        {
            return SpotInput.Empty;
        }

        return new SpotInput()
        {
            RoomId = spot.GetComponentInParent<Room>().transform.GetSiblingIndex(),
            SpotId = spot.transform.GetSiblingIndex()
        };
    }
}