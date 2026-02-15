using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    public void OnSearchAnimationCompleted()
    {
        GetComponentInParent<PlayerNetState>().SetIdleServerRpc();
    }

    public void OnPutAnimationCompleted()
    {
        GetComponentInParent<PlayerNetState>().SetIdleServerRpc();
    }

    public void OnBeingExaminedCompleted()
    {
        GetComponentInParent<PlayerNetState>().SetIdleServerRpc();
    }
}
