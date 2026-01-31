using System;
using GameState;
using Unity.Netcode;
using UnityEngine;

namespace Teams
{
    public class PlayerTeamController : NetworkBehaviour
    {
        public NetworkVariable<Team> AssignedTeam { get; } = new(
            Team.Unassigned,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action<Team> OnTeamAssigned;

        public bool CanPerformActions
        {
            get
            {
                if (GameStateManager.Instance == null)
                {
                    return false;
                }

                var phase = GameStateManager.Instance.CurrentPhase.Value;

                switch (phase)
                {
                    case GamePhase.WaitingForAssignment:
                    case GamePhase.TeamReveal:
                        return false;

                    case GamePhase.NabuWaiting:
                        return AssignedTeam.Value == Team.CorruptOfficials;

                    case GamePhase.MainGameplay:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            AssignedTeam.OnValueChanged += HandleTeamChanged;

            if (AssignedTeam.Value != Team.Unassigned)
            {
                OnTeamAssigned?.Invoke(AssignedTeam.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            AssignedTeam.OnValueChanged -= HandleTeamChanged;
            base.OnNetworkDespawn();
        }

        private void HandleTeamChanged(Team previousValue, Team newValue)
        {
            Debug.Log($"[PlayerTeamController] Team changed from {previousValue} to {newValue} for client {OwnerClientId}");
            OnTeamAssigned?.Invoke(newValue);
        }

        public static PlayerTeamController GetLocalPlayer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            {
                return null;
            }

            var localClientId = NetworkManager.Singleton.LocalClientId;

            foreach (var player in FindObjectsByType<PlayerTeamController>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == localClientId)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
