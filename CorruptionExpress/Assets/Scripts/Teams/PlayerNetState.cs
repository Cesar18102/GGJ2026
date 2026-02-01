using Unity.Netcode;
using UnityEngine;

public class PlayerNetState : NetworkBehaviour
{
    public NetworkVariable<bool> IsActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> EvidenceCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

    [Header("What to hide/disable")]
    [SerializeField] private GameObject visualsRoot; // спрайти/скелет/рендер
    [SerializeField] private Collider2D[] collidersToDisable;

    public override void OnNetworkSpawn()
    {
        Apply(IsActive.Value);
        IsActive.OnValueChanged += (_, v) => Apply(v);
    }

    public override void OnNetworkDespawn()
    {
        IsActive.OnValueChanged -= (_, v) => Apply(v); // не критично для джему, але ок
    }

    private void Apply(bool active)
    {
        if (visualsRoot != null)
            visualsRoot.SetActive(active);

        if (collidersToDisable != null)
        {
            foreach (var c in collidersToDisable)
                if (c != null) c.enabled = active;
        }
    }
}
