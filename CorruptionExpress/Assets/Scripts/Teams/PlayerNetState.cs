using Assets.Scripts.Actions;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetState : NetworkBehaviour
{
    public NetworkVariable<int> EvidenceCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> CurrentPosition = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> CurrentRoom = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<AnimationType> CurrentAnimationType = new(
        AnimationType.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public FaceDirection CurrentFaceDirection { get; set; }
    public float Speed { get; set; }

    public void AddEvidence(int amount)
    {
        Debug.Log($"Providing {amount} items.");
        EvidenceCount.Value += amount;
        Debug.Log($"{amount} items provided.");
    }

    [ServerRpc]
    public void SpendEvidenceServerRpc(int amount = 1)
    {
        if (EvidenceCount.Value <= 0)
        {
            return;
        }

        EvidenceCount.Value -= amount;
    }

    public override void OnNetworkSpawn()
    {
        CurrentAnimationType.OnValueChanged += OnAnimtionTypeUpdated;
    }

    public override void OnNetworkDespawn()
    {
        CurrentAnimationType.OnValueChanged -= OnAnimtionTypeUpdated;
    }

    private void OnAnimtionTypeUpdated(AnimationType oldState, AnimationType newState)
    {
        GetComponentInChildren<Animator>().SetInteger("State", (int)newState);
    }
}
