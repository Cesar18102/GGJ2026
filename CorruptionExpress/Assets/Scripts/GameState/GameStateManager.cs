using Assets.Scripts.Helpers;
using Assets.Scripts.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Teams;
using Unity.Netcode;
using UnityEngine;

namespace GameState
{
    public class GameStateManager : NetworkBehaviour
    {
        [SerializeField]
        private int _numberOfEvidences;

        [SerializeField]
        private GameObject _evidencePrefab;

        [SerializeField]
        private UIController _uiController;

        [SerializeField] 
        private Transform[] _spawnPoints;

        [SerializeField]
        private int _planningCycles = 3;

        [SerializeField]
        private int _totalGameRounds = 5;

        [SerializeField]
        private SceneInputController _sceneInputController;

        [SerializeField]
        private GameObject _roomsContainer;

        private Dictionary<ulong, InputData> _inputs = new Dictionary<ulong, InputData>();

        public static GameStateManager Instance { get; private set; }

        // чей сейчас ход (clientId)
        public NetworkVariable<ulong> CurrentTurnClientId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // какой круг планирования
        public NetworkVariable<int> PlanningRound = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // последнее действие (для UI всем)
        public NetworkVariable<ActionType> LastPlannedAction = new(
            ActionType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<ulong> LastActionByClientId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> ExecutionRound = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkList<ulong> TurnOrder { get; } = new NetworkList<ulong>();
        public NetworkList<PlannedAction> PlannedActions { get; } = new NetworkList<PlannedAction>();

        public NetworkVariable<int> ExecIndex = new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<ActionType> CurrentExecutedAction = new(
            ActionType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<ulong> CurrentExecutedByClientId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<GamePhase> CurrentPhase { get; } = new(
            GamePhase.WaitingForAssignment,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public event Action<GamePhase> OnPhaseChanged;

        public NetworkVariable<int> RemainingEvidences { get; } = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );

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

            RemainingEvidences.OnValueChanged += HandleRemainingNumberOfItemsChanged;
            CurrentPhase.OnValueChanged += HandlePhaseChanged;
            _uiController.OnInput += OnUiInput;

            if (IsServer)
            {
                StartCoroutine(GameFlowCoroutine());
            }

            Debug.Log($"[GameStateManager] Network spawned. Current phase: {CurrentPhase.Value}");
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"[GameStateManager] Network despawned.");

            RemainingEvidences.OnValueChanged -= HandleRemainingNumberOfItemsChanged;
            CurrentPhase.OnValueChanged -= HandlePhaseChanged;
            _uiController.OnInput -= OnUiInput;

            TurnOrder.Dispose();

            if (Instance == this)
            {
                Instance = null;
            }

            base.OnNetworkDespawn();
        }

        private IEnumerator GameFlowCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            if (TeamManager.Instance != null)
            {
                TeamManager.Instance.AssignTeams();
                //RemainingEvidences.Value = _numberOfEvidences * TeamManager.Instance.GetCountCorruption();
            }
            else
            {
                Debug.LogError("[GameStateManager] TeamManager not found!");
                yield break;
            }

            PlayerTeamController.ForEachPlayer(player => player.transform.SetPositionAndRotation(Vector3.up * 100, Quaternion.identity));

            yield return new WaitForSeconds(0.5f);
            yield return SetupPhase();

            for (int i = 0; i < _totalGameRounds; ++i)
            {
                Debug.Log($"Game Round: {i + 1}");

                yield return PlanningPhase();
                yield return ExecutionPhase();
            }
        }

        private void SetPhase(GamePhase newPhase)
        {
            if (!IsServer)
            {
                return;
            }

            Debug.Log($"[GameStateManager] Setting phase to {newPhase}");
            CurrentPhase.Value = newPhase;
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            Debug.Log($"[GameStateManager] Phase changed from {previousPhase} to {newPhase}");
            OnPhaseChanged?.Invoke(newPhase);
        }

        [Rpc(SendTo.Server)]
        public void HandleInputServerRpc(InputData input, RpcParams rpcParams = default)
        {
            if (IsServer)
            {
                Debug.Log($"[GameStateManager] Server recieved input: " +
                    $"Spot(R:{input.SpotInput.RoomId},S:{input.SpotInput.SpotId}); " +
                    $"TargetPlayer({input.TargetClientId}); " +
                    $"Action({input.ActionType}); " +
                    $"Move({input.MoveDirection})"
                );

                _inputs[rpcParams.Receive.SenderClientId] = input;
            }
        }

        private void OnUiInput(object sender, InputData e)
        {
            Debug.Log($"[UI_SEND] local={NetworkManager.Singleton.LocalClientId} current(localView)={CurrentTurnClientId.Value} phase={CurrentPhase.Value}");
            HandleInputServerRpc(e);
        }

        #region Gameplay

        #region Setup Phase

        private IEnumerator SetupPhase()
        {
            SetPhase(GamePhase.Setup);
            ShowPreparePanelClientRpc();
            SetTeamActive(Team.CorruptOfficials, true);
            SetTeamActive(Team.Nabu, false);

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged += PlayerItemsCountChanged);

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().AddEvidence(_numberOfEvidences));
            Debug.Log("[SETUP] Corrupt evidence counts: " + string.Join(", ", PlayerTeamController.Select(Team.CorruptOfficials, p => p.GetComponent<PlayerNetState>().EvidenceCount.Value)));

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => StartCoroutine(HideEvidenceCo(player.GetComponent<PlayerTeamController>())));
            yield return new WaitUntil(() => PlayerTeamController.Select(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.Value == 0).All(value => value));

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged -= PlayerItemsCountChanged);

            SetTeamActive(Team.Nabu, true);
            SpawnPlayersRandom();
            DefineOrder();
        }

        private void PlayerItemsCountChanged(int oldItems, int newItems)
        {
            if (IsServer)
            {
                Debug.Log($"Player Items Count Changed from {oldItems} to {newItems}");

                int diff = oldItems - newItems;
                RemainingEvidences.Value -= diff;
            }
        }

        private void HandleRemainingNumberOfItemsChanged(int oldItems, int newItems)
        {
            Debug.Log($"Remaining Items Count Changed from {oldItems} to {newItems}");
            _uiController.SetRemainingItemsToWait(newItems);
        }

        private void SetTeamActive(Team team, bool active)
        {
            PlayerTeamController.ForEachPlayer(team, player => player.GetComponent<PlayerNetState>().IsActive.Value = active);
        }

        [Rpc(SendTo.Everyone)]
        private void ShowPreparePanelClientRpc()
        {
            PlayerTeamController currentPlayer = PlayerTeamController.GetLocalPlayer();
            _uiController.ShowPreparePanel(currentPlayer.AssignedTeam.Value);
        }

        private IEnumerator HideEvidenceCo(PlayerTeamController player)
        {
            PlayerNetState state = player.GetComponent<PlayerNetState>();

            while (state.EvidenceCount.Value > 0)
            {
                ulong clientId = player.OwnerClientId;

                yield return WaitForInput(clientId, data => data.HasSpotInput());
                Spot spot = GetSpot(_inputs[clientId].SpotInput);

                state.EvidenceCount.Value--;
                spot.GetComponent<SpotNetState>().HasItem.Value = true;
            }
        }

        private void SpawnPlayersRandom()
        {
            Debug.Log($"Players are being spawned");

            _spawnPoints.Shuffle();

            PlayerTeamController.ForEachPlayer((player, i) =>
            {
                Transform p = _spawnPoints[i % _spawnPoints.Length];
                player.transform.SetPositionAndRotation(p.position, p.rotation);

                // якщо треба для 2D: обнулити Z
                var pos = player.transform.position;
                pos.z = 0f;
                player.transform.position = pos;

                PlayerNetState state = player.GetComponent<PlayerNetState>();
                state.CurrentPosition.Value = p.gameObject.transform.GetSiblingIndex();
                state.CurrentRoom.Value = p.gameObject.GetComponentInParent<Room>().gameObject.transform.GetSiblingIndex();
            });

            SpawnPlayersClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void SpawnPlayersClientRpc()
        {
            PlayerTeamController player = PlayerTeamController.GetLocalPlayer();
            PlayerNetState state = player.NetworkObject.GetComponent<PlayerNetState>();
            NavNode2D navNode = GetRoom(state.CurrentRoom.Value).GetWaypoint(state.CurrentPosition.Value);

            player.gameObject.GetComponentInChildren<CharacterNavigationController>().SetCurrentNavNode(navNode);
        }

        private void DefineOrder()
        {
            var clients = PlayerTeamController.Select(player => player.OwnerClientId).ToArray();
            clients.Shuffle();

            TurnOrder.Clear();
            foreach (var id in clients)
            {
                TurnOrder.Add(id);
            }

            Debug.Log($"Turn Order: {string.Join(",", TurnOrder)}");
        }

        #endregion Setup Phase

        #region Planning Phase

        private IEnumerator PlanningPhase()
        {
            PlannedActions.Clear();

            SetPhase(GamePhase.Planning);

            PlayerTeamController[] players = TurnOrder.AsNativeArray().Select(id => PlayerTeamController.GetPlayer(id)).ToArray();

            for (PlanningRound.Value = 0; PlanningRound.Value < _planningCycles; PlanningRound.Value++)
            {
                Debug.Log($"Planning round: {PlanningRound.Value} started.");
                for(int turnIndex = 0; turnIndex < players.Length; turnIndex++) {
                    Debug.Log($"Current turn: {turnIndex}");

                    ulong playerId = players[turnIndex].OwnerClientId;
                    CurrentTurnClientId.Value = playerId;
                    
                    yield return WaitForInput(playerId, input => input.HasActionTypeInput());

                    ActionType action = _inputs[playerId].ActionType;
                    PlannedAction plannedAction = new PlannedAction(playerId, action);
                    PlannedActions.Add(plannedAction);

                    Debug.Log($"{action} planned by {playerId}");

                    LastPlannedAction.Value = action;
                    LastActionByClientId.Value = playerId;
                }
            }

            Debug.Log("[Planning] Done. Queue size=" + PlannedActions.Count);
        }

        #endregion Planning Phase

        #region Execution Phase

        private IEnumerator ExecutionPhase()
        {
            SetPhase(GamePhase.Execution);

            for (ExecIndex.Value = 0; ExecIndex.Value < PlannedActions.Count; ExecIndex.Value++)
            {
                var step = PlannedActions[ExecIndex.Value];

                CurrentExecutedAction.Value = step.Action;
                CurrentExecutedByClientId.Value = step.ClientId;

                PlayerTeamController player = PlayerTeamController.GetPlayer(step.ClientId);
                PlayerNetState state = player.NetworkObject.GetComponent<PlayerNetState>();

                state.IsExecuting.Value = true;

                if (step.Action == ActionType.Move)
                {
                    yield return WaitForInput(step.ClientId, input => input.HasSpotInput());
                    NavigateClientRpc(step.ClientId, _inputs[step.ClientId].SpotInput);
                }

                yield return new WaitUntil(() => !state.IsExecuting.Value);
            }

            ExecIndex.Value = -1;
            PlannedActions.Clear();
        }

        [Rpc(SendTo.Everyone)]
        private void NavigateClientRpc(ulong clientId, SpotInput spotInfo)
        {
            StartCoroutine(NavigateClientCo(clientId, spotInfo));
        }

        private IEnumerator NavigateClientCo(ulong clientId, SpotInput spotInfo)
        {
            PlayerTeamController player = PlayerTeamController.GetPlayer(clientId);
            Spot spot = GetSpot(spotInfo);

            CharacterNavigationController navController = player.GetComponentInChildren<CharacterNavigationController>();
            yield return navController.GoTo(spot);

            player.NetworkObject.GetComponent<PlayerNetState>().SetIsExecutingServerRpc(false);
        }

        #endregion Execution Phase

        #endregion Gameplay

        private IEnumerator WaitForInput(ulong clientId, Func<InputData, bool> inputIndicator)
        {
            _inputs.Remove(clientId);
            yield return new WaitUntil(() => _inputs.ContainsKey(clientId) && inputIndicator(_inputs[clientId]));
        }

        private Room GetRoom(int index)
        {
            return _roomsContainer.transform.GetChild(index).gameObject.GetComponent<Room>();
        }

        private Spot GetSpot(SpotInput spotInput)
        {
            return GetRoom(spotInput.RoomId).GetSpot(spotInput.SpotId);
        }
    }
}
