using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    public void OnSearchAnimationCompleted()
    {
        GetComponentInParent<PlayerNetState>().SetIdleServerRpc();
    }
}
