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
           

        public static void ForEachPlayer(Action<NetworkObject> action) => 
            ForEachPlayer(player => true, (player, i) => action(player));
        public static void ForEachPlayer(Action<NetworkObject, int> action) =>
            ForEachPlayer(player => true, action);

        public static void ForEachPlayer(Team team, Action<NetworkObject> action) => 
            ForEachPlayer(team, (player, i) => action(player));
        public static void ForEachPlayer(Team team, Action<NetworkObject, int> action) =>
            ForEachPlayer(player => player.GetComponent<PlayerTeamController>()?.AssignedTeam.Value == team, action);

        public static void ForEachPlayer(Predicate<NetworkObject> playerPredicate, Action<NetworkObject> action) => 
            ForEachPlayer(playerPredicate, (player, i) => action(player));
        public static void ForEachPlayer(Predicate<NetworkObject> playerPredicate, Action<NetworkObject, int> action)
        {
            var clients = NetworkManager.Singleton.ConnectedClientsList;

            for (int i = 0; i < clients.Count; ++i)
            {
                var client = clients[i];
                var playerObj = client.PlayerObject;
                if (playerObj == null || !playerPredicate(playerObj))
                {
                    continue;
                }

                action(playerObj, i);
            }
        }

        public static IEnumerable<T> Select<T>(Func<NetworkObject, T> selector) => 
            Select(player => true, selector);

        public static IEnumerable<T> Select<T>(Team team, Func<NetworkObject, T> selector) =>
            Select(player => player.GetComponent<PlayerTeamController>()?.AssignedTeam.Value == team, selector);

        public static IEnumerable<T> Select<T>(Predicate<NetworkObject> playerPredicate, Func<NetworkObject, T> selector)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObj = client.PlayerObject;
                if (playerObj == null || !playerPredicate(playerObj))
                {
                    continue;
                }

                yield return selector(playerObj);
            }
        }
    }
}
