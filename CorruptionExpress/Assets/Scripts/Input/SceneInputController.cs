using UnityEngine;
using UnityEngine.InputSystem;

public class SceneInputController : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private GameActionHandler _currentGameActionHandler;

    private Vector2 _lastPointerPos;

    public void SetCurrentGameActionHandler(GameActionHandler currentGameActionHandler)
    {
        _currentGameActionHandler = currentGameActionHandler;
    }

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

        if (_currentGameActionHandler != null && collider?.gameObject != null)
        {
            _currentGameActionHandler.OnSceneObjectClicked(collider.gameObject);
        }
    }
}