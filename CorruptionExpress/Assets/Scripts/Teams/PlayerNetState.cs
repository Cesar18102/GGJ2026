using Assets.Scripts.Actions;
using Assets.Scripts.Interface;
using Assets.Scripts.Navigation;
using System;
using Teams;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerNetState : NetworkBehaviour, ISearchable
{
    public NetworkVariable<Team> AssignedTeam { get; } = new(
        Team.Unassigned,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public event Action<Team> OnTeamAssigned;

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
    public NetworkVariable<bool> WearsMask = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<bool> IsDeanonimized = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] int _orderOffset = 0;
    [SerializeField] float _scale = 100f;

    public FaceDirection CurrentFaceDirection { get; set; }
    public float Speed { get; set; }

    public bool CanTakeItem => EvidenceCount.Value < 4;
    public bool CanPutItem => EvidenceCount.Value > 0;

    void LateUpdate()
    {
        SortingGroup group = GetComponentInChildren<SortingGroup>();
        if (group != null)
        {
            group.sortingOrder = _orderOffset + Mathf.RoundToInt(-NetworkObject.transform.position.y * _scale);
        }
    }

    public Vector3 GetApproachPosition() => GetComponentInChildren<CharacterSettings>().GetApproachPosition().position;

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

    [Rpc(SendTo.Server)]
    public void SetIdleServerRpc()
    {
        CurrentAnimationType.Value = AnimationType.None;
    }

    public override void OnNetworkSpawn()
    {
        CurrentAnimationType.OnValueChanged += OnAnimtionTypeUpdated;
        AssignedTeam.OnValueChanged += HandleTeamChanged;
        WearsMask.OnValueChanged += OnWearsMaskChanged;
        IsDeanonimized.OnValueChanged += OnIsDeanonimizedChanged;

        if (AssignedTeam.Value != Team.Unassigned)
        {
            OnTeamAssigned?.Invoke(AssignedTeam.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentAnimationType.OnValueChanged -= OnAnimtionTypeUpdated;
        AssignedTeam.OnValueChanged -= HandleTeamChanged;
        WearsMask.OnValueChanged -= OnWearsMaskChanged;
        IsDeanonimized.OnValueChanged -= OnIsDeanonimizedChanged;
    }

    private void HandleTeamChanged(Team previousValue, Team newValue)
    {
        Debug.Log($"[PlayerNetState] Team changed from {previousValue} to {newValue} for client {OwnerClientId}");
        OnTeamAssigned?.Invoke(newValue);
    }

    private void OnWearsMaskChanged(bool oldState, bool newState)
    {
        if (AssignedTeam.Value != Team.Nabu)
        {
            return;
        }

        NabuAppearanceController nabuAppearanceController = GetComponentInChildren<NabuAppearanceController>();

        if (newState)
        {
            nabuAppearanceController.WearMask();
        }
        else
        {
            nabuAppearanceController.TakeMaskOff();
        }
    }

    private void OnIsDeanonimizedChanged(bool oldState, bool newState)
    {
        if (AssignedTeam.Value != Team.Nabu)
        {
            return;
        }

        if (newState)
        {
            GetComponentInChildren<NabuAppearanceController>().Deanonimize();
        }
    }

    private void OnAnimtionTypeUpdated(AnimationType oldState, AnimationType newState)
    {
        GetComponentInChildren<Animator>().SetInteger("State", (int)newState);
    }

    public bool TakeItem()
    {
        if (EvidenceCount.Value > 0)
        {
            EvidenceCount.Value--;
            return true;
        }

        return false;
    }
}
