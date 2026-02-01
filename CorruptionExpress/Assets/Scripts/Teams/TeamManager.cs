using Assets.Scripts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Teams
{
    public class TeamManager : NetworkBehaviour
    {
        public static TeamManager Instance { get; private set; }

        public event Action OnTeamsAssigned;

        private readonly Dictionary<ulong, Team> _clientTeamAssignments = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("[TeamManager] Network spawned");
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnNetworkDespawn();
        }

        public void AssignTeams()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[TeamManager] Only the server can assign teams");
                return;
            }

            var clientIds = GetConnectedClientIds();
            var playerCount = clientIds.Count;

            if (playerCount == 0)
            {
                Debug.LogWarning("[TeamManager] No players to assign teams to");
                return;
            }

            var nabuCount = CalculateNabuCount(playerCount);

            Debug.Log($"[TeamManager] Assigning teams: {playerCount} players, {nabuCount} Nabu, {playerCount - nabuCount} Corrupt Officials");

            clientIds.Shuffle();

            _clientTeamAssignments.Clear();

            for (var i = 0; i < clientIds.Count; i++)
            {
                var clientId = clientIds[i];
                var team = i < nabuCount ? Team.Nabu : Team.CorruptOfficials;
                _clientTeamAssignments[clientId] = team;

                AssignTeamToPlayer(clientId, team);
            }

            OnTeamsAssigned?.Invoke();
            NotifyTeamsAssignedClientRpc();
        }

        private void AssignTeamToPlayer(ulong clientId, Team team)
        {
            foreach (var player in FindObjectsByType<PlayerTeamController>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == clientId)
                {
                    player.AssignedTeam.Value = team;
                    Debug.Log($"[TeamManager] Assigned {team} to client {clientId}");
                    return;
                }
            }

            Debug.LogWarning($"[TeamManager] Could not find PlayerTeamController for client {clientId}");
        }

        [ClientRpc]
        private void NotifyTeamsAssignedClientRpc()
        {
            Debug.Log("[TeamManager] Teams have been assigned");
            OnTeamsAssigned?.Invoke();
        }

        public Team GetTeamForClient(ulong clientId)
        {
            return _clientTeamAssignments.TryGetValue(clientId, out var team) ? team : Team.Unassigned;
        }

        public static int CalculateNabuCount(int playerCount)
        {
            return Mathf.Max(1, Mathf.FloorToInt(playerCount * 0.68f));
        }

        public int GetCountCorruption()
        {
            return _clientTeamAssignments.Where(item => item.Value == Team.CorruptOfficials).Count();
        }

        private List<ulong> GetConnectedClientIds()
        {
            var clientIds = new List<ulong>();

            if (NetworkManager.Singleton == null)
            {
                return clientIds;
            }

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                clientIds.Add(clientId);
            }

            return clientIds;
        }
    }
}
