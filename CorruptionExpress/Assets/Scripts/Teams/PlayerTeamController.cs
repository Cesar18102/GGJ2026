using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
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

            return GetPlayer(NetworkManager.Singleton.LocalClientId);
        }

        public static PlayerTeamController GetPlayer(ulong clientId)
        {
            foreach (var player in FindObjectsByType<PlayerTeamController>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == clientId)
                {
                    return player;
                }
            }

            return null;
        }
           

        
    }
}
