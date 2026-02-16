using Assets.Scripts.ActionHandlers;
using Assets.Scripts.Actions;
using Assets.Scripts.GameState;
using Assets.Scripts.Helpers;
using Assets.Scripts.Input;
using Assets.Scripts.Interface;
using Assets.Scripts.Navigation;
using Assets.Scripts.UI;
using Rooms;
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
        private float _evidenceRatioFoundForNabuWin = 0.7f;

        [SerializeField]
        private float _deanonRatioForNabuLose = 1.0f;

        [SerializeField]
        private SceneInputController _sceneInputController;

        [SerializeField]
        private GameObject _roomsContainer;

        [SerializeField]
        private RoomController _roomController;

        private int _totalEvidences = 0;

        private Dictionary<ulong, InputData> _inputs = new Dictionary<ulong, InputData>();

        public static GameStateManager Instance { get; private set; }

        public NetworkVariable<int> GameRound { get; } = new(
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

        #region Setup variables
        public NetworkVariable<int> RemainingEvidences { get; } = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        #endregion Setup variables

        #region Planning variables
        public NetworkVariable<long> CurrentTurnClientId = new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> PlanningRound = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkList<ulong> TurnOrder { get; } = new NetworkList<ulong>();
        public NetworkList<PlannedAction> PlannedActions { get; } = new NetworkList<PlannedAction>();
        #endregion Planning variables

        #region Execution variables
        public NetworkVariable<int> ExecIndex = new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<Team> WinTeam = new(
            Team.Unassigned,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<WinReason> Reason = new(
            WinReason.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #endregion Execution variables

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
            }
            else
            {
                Debug.LogError("[GameStateManager] TeamManager not found!");
                yield break;
            }

            PlayersHelper.ForEachPlayer(player => player.transform.SetPositionAndRotation(Vector3.up * 100, Quaternion.identity));
            SubscribePlayerInventoryChangedClientRpc();

            yield return new WaitForSeconds(0.5f);
            yield return SetupPhase();

            SubscribeCurrentRoomChangedClientRpc();

            for (GameRound.Value = 0; GameRound.Value < _totalGameRounds; ++GameRound.Value)
            {
                Debug.Log($"Game Round: {GameRound.Value + 1}");

                yield return PlanningPhase();
                yield return ExecutionPhase();

                if (WinTeam.Value != Team.Unassigned)
                {
                    break;
                }
            }

            if (WinTeam.Value == Team.Unassigned)
            {
                WinTeam.Value = Team.CorruptOfficials;
                Reason.Value = WinReason.RoundsPassed;
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

            PlayersHelper.ForEachPlayer(Team.Nabu, player => player.GetComponent<PlayerNetState>().WearsMask.Value = true);
            PlayersHelper.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().WearsMask.Value = false);

            PlayersHelper.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged += PlayerItemsCountChanged);

            PlayersHelper.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().AddEvidence(_numberOfEvidences));
            Debug.Log("[SETUP] Corrupt evidence counts: " + string.Join(", ", PlayersHelper.Select(Team.CorruptOfficials, p => p.GetComponent<PlayerNetState>().EvidenceCount.Value)));

            _totalEvidences = PlayersHelper.Select(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.Value).Sum();

            SetNavigationEnabledClientRpc(true);

            PlayersHelper.ForEachPlayer(Team.CorruptOfficials, player => StartCoroutine(HideEvidenceCo(player.GetComponent<PlayerNetState>())));
            yield return new WaitUntil(() => PlayersHelper.Select(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.Value == 0).All(value => value));

            SetNavigationEnabledClientRpc(false);

            PlayersHelper.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged -= PlayerItemsCountChanged);

            SpawnPlayersRandom();
            DefineOrder();
        }

        [Rpc(SendTo.Everyone)]
        private void SetNavigationEnabledClientRpc(bool isEnabled)
        {
            _roomController.SetNavigationEnabled(isEnabled);
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

        [Rpc(SendTo.Everyone)]
        private void ShowPreparePanelClientRpc()
        {
            PlayerNetState currentPlayer = PlayersHelper.GetLocalPlayer();
            _uiController.ShowPreparePanel(currentPlayer.AssignedTeam.Value);
            _uiController.StartMusic(currentPlayer.AssignedTeam.Value);
        }

        private IEnumerator HideEvidenceCo(PlayerNetState player)
        {
            PlayerNetState state = player.GetComponent<PlayerNetState>();

            while (state.EvidenceCount.Value > 0)
            {
                ulong clientId = player.OwnerClientId;

                yield return WaitForInput(clientId, data => data.HasSpotInput());
                Spot spot = GetSpot(_inputs[clientId].SpotInput);

                state.EvidenceCount.Value--;
                spot.GetComponent<SpotNetState>().ItemsCount.Value++;
            }
        }

        private void SpawnPlayersRandom()
        {
            Debug.Log($"Players are being spawned");

            _spawnPoints.Shuffle();

            PlayersHelper.ForEachPlayer((player, i) =>
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
                state.CurrentFaceDirection = player.GetComponentInChildren<CharacterSettings>().GetStartingFaceDirection();
                state.Speed = player.GetComponentInChildren<CharacterSettings>().GetSpeed();

                player.transform.localScale = Vector3.one * GetCurrentWaypoint(state).GetDesiredScale();
            });
        }

        private void DefineOrder()
        {
            var clients = PlayersHelper.Select(player => player.OwnerClientId).ToArray();
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
            SetPhase(GamePhase.Planning);

            PlayerNetState[] players = TurnOrder.AsNativeArray().Select(id => PlayersHelper.GetPlayer(id)).ToArray();

            for (PlanningRound.Value = 0; PlanningRound.Value < _planningCycles; PlanningRound.Value++)
            {
                Debug.Log($"Planning round: {PlanningRound.Value} started.");
                for(int turnIndex = 0; turnIndex < players.Length; turnIndex++) {
                    ulong playerId = players[turnIndex].OwnerClientId;
                    Debug.Log($"Current turn: {turnIndex}; Planning by {playerId}");

                    CurrentTurnClientId.Value = (long)playerId;
                    
                    yield return WaitForInput(playerId, input => input.HasActionTypeInput());

                    ActionType action = _inputs[playerId].ActionType;
                    PlannedAction plannedAction = new PlannedAction(playerId, action);
                    PlannedActions.Add(plannedAction);

                    Debug.Log($"{action} planned by {playerId}");
                }
            }

            CurrentTurnClientId.Value = -1;
            Debug.Log("[Planning] Done. Queue size=" + PlannedActions.Count);
        }

        #endregion Planning Phase

        #region Execution Phase

        private IEnumerator ExecutionPhase()
        {
            SetPhase(GamePhase.Execution);

            for (ExecIndex.Value = 0; ExecIndex.Value < PlannedActions.Count; ExecIndex.Value++)
            {
                PlannedAction step = PlannedActions[ExecIndex.Value];
                PlayerNetState player = PlayersHelper.GetPlayer(step.ClientId);

                Debug.Log($"[Execution] turn {ExecIndex.Value}: {step.Action} by {step.ClientId}");

                if (step.Action == ActionType.Move)
                {
                    yield return WaitForInput(step.ClientId, input => input.HasMoveDirectionInput());

                    RoomMoveDirection direction = _inputs[step.ClientId].MoveDirection;

                    Room currentRoom = GetRoom(player.CurrentRoom.Value);
                    NavNode2D currentNode = currentRoom.GetWaypoint(player.CurrentPosition.Value);
                    NavNode2D exit = currentRoom.GetExit(direction);

                    yield return MoveActionHandler.MoveCo(player, currentNode, exit);

                    int newRoomIndex = player.CurrentRoom.Value + direction.ToRoomIndexDelta();
                    Room newRoom = GetRoom(newRoomIndex);
                    NavNode2D entrance = newRoom.GetEntrance(direction);

                    yield return MoveActionHandler.MoveCo(player, exit, entrance);

                    player.CurrentRoom.Value = newRoomIndex;
                }

                if (step.Action == ActionType.Search)
                {
                    yield return WaitForInput(step.ClientId, input => input.HasSpotInput() || (player.AssignedTeam.Value == Team.Nabu && input.HasTargetPlayerInput()));

                    InputData input = _inputs[step.ClientId];
                    NavNode2D currentNode = GetCurrentWaypoint(player);

                    if (input.HasSpotInput())
                    {
                        Spot spot = GetSpot(input.SpotInput);

                        yield return MoveActionHandler.MoveCo(player, currentNode, spot.GetApproachNode());
                        MoveActionHandler.UpdateFaceDirection(player, spot.GetFaceDirection());

                        player.CurrentAnimationType.Value = AnimationType.Search;
                        yield return new WaitUntil(() => player.CurrentAnimationType.Value == AnimationType.None);

                        if (player.CanTakeItem && spot.TakeItem())
                        {
                            player.AddEvidence(1);
                        }
                    }
                    else if (input.HasTargetPlayerInput())
                    {
                        PlayerNetState targetPlayer = PlayersHelper.GetPlayer((ulong)input.TargetClientId);
                        NavNode2D targetNode = GetCurrentWaypoint(targetPlayer);
                        Vector3 stepAsidePosition = targetPlayer.GetApproachPosition();

                        yield return MoveActionHandler.MoveCo(player, currentNode, targetNode, stepAsidePosition);
                        MoveActionHandler.UpdateFaceDirection(player, targetPlayer.CurrentFaceDirection.Invert());

                        player.CurrentAnimationType.Value = AnimationType.SearchPlayer;
                        targetPlayer.CurrentAnimationType.Value = AnimationType.BeingSearched;
                        yield return new WaitUntil(() => player.CurrentAnimationType.Value == AnimationType.None);
                        targetPlayer.CurrentAnimationType.Value = AnimationType.None;

                        if (player.CanTakeItem && targetPlayer.TakeItem())
                        {
                            player.AddEvidence(1);
                        }
                    }
                }

                if (step.Action == ActionType.Put)
                {
                    yield return WaitForInput(step.ClientId, input => input.HasSpotInput());

                    NavNode2D currentNode = GetCurrentWaypoint(player);

                    Spot spot = GetSpot(_inputs[step.ClientId].SpotInput);
                    NavNode2D targetNode = spot.GetApproachNode();

                    yield return MoveActionHandler.MoveCo(player, currentNode, targetNode);
                    MoveActionHandler.UpdateFaceDirection(player, spot.GetFaceDirection());

                    player.CurrentAnimationType.Value = AnimationType.Put;
                    yield return new WaitUntil(() => player.CurrentAnimationType.Value == AnimationType.None);

                    if (player.CanPutItem)
                    {
                        spot.PutItem();
                        player.EvidenceCount.Value--;
                    }
                }

                if (step.Action == ActionType.Wear)
                {
                    yield return new WaitForSeconds(0.5f);

                    if (!player.IsDeanonimized.Value)
                    {
                        player.WearsMask.Value = !player.WearsMask.Value;
                    }

                    yield return new WaitForSeconds(0.5f);
                }

                DeanonimizationCheck(player.CurrentRoom.Value);

                WinTeam.Value = WinCheck(out WinReason reason);
                Reason.Value = reason;

                if (WinTeam.Value != Team.Unassigned)
                {
                    break;
                }
            }

            ExecIndex.Value = -1;
            PlannedActions.Clear();
        }

        #endregion Execution Phase

        #region Checks

        private void DeanonimizationCheck(int room)
        {
            PlayerNetState[] playersInSameRoom = PlayersHelper.Select(
                player => player.GetComponent<PlayerNetState>().CurrentRoom.Value == room, 
                player => player.GetComponent<PlayerNetState>()
            ).ToArray();

            bool anyCorruptionersHere = playersInSameRoom.Any(player => player.AssignedTeam.Value == Team.CorruptOfficials);
            if (anyCorruptionersHere)
            {
                IEnumerable<PlayerNetState> nabuAgentsToDeanonimize = playersInSameRoom.Where(player => player.AssignedTeam.Value == Team.Nabu && !player.WearsMask.Value);
                foreach (PlayerNetState nabuAgent in nabuAgentsToDeanonimize)
                {
                    nabuAgent.IsDeanonimized.Value = true;
                }
            }
        }

        private Team WinCheck(out WinReason reason)
        {
            int foundEvidences = PlayersHelper.Select(Team.Nabu, player => player.GetComponent<PlayerNetState>().EvidenceCount.Value).Sum();
            int neededEvidences = (int)Math.Round(_totalEvidences * _evidenceRatioFoundForNabuWin);

            int deanonCount = PlayersHelper.Select(Team.Nabu, player => player.GetComponent<PlayerNetState>().IsDeanonimized.Value).Count(value => value);
            int totalNabu = PlayersHelper.Select(Team.Nabu, player => player).Count();
            int neededDeanonToLose = (int)Math.Round(totalNabu * _deanonRatioForNabuLose);

            if (deanonCount >= neededDeanonToLose)
            {
                reason = WinReason.Deanon;
                return Team.CorruptOfficials;
            }

            if (foundEvidences >= neededEvidences)
            {
                reason = WinReason.EvidencesFound;
                return Team.Nabu;
            }

            reason = WinReason.None;
            return Team.Unassigned;
        }

        #endregion Checks

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

        private NavNode2D GetCurrentWaypoint(PlayerNetState player)
        {
            return GetRoom(player.CurrentRoom.Value).GetWaypoint(player.CurrentPosition.Value);
        }

        [Rpc(SendTo.Everyone)]
        private void SubscribePlayerInventoryChangedClientRpc()
        {
            PlayersHelper.GetLocalPlayer().EvidenceCount.OnValueChanged += OnInventoryChanged;
        }

        private void OnInventoryChanged(int oldCount, int newCount)
        {
            _uiController.UpdateItems(newCount);
        }


        [Rpc(SendTo.Everyone)]
        private void SubscribeCurrentRoomChangedClientRpc()
        {
            PlayersHelper.GetLocalPlayer().CurrentRoom.OnValueChanged += OnPlayerRoomChanged;
        }

        private void OnPlayerRoomChanged(int oldRoom, int newRoom)
        {
            PlayerNetState player = PlayersHelper.GetLocalPlayer();
            Debug.Log($"Player {player.OwnerClientId} changed room from {oldRoom} to {newRoom}");

            _roomController.NavigateToRoom(newRoom);
        }

        private void FixedUpdate()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            PlayerNetState player = PlayersHelper.GetLocalPlayer();

            if (player == null)
            {
                return;
            }

            bool isHidingItems = 
                CurrentPhase.Value == GamePhase.Setup && 
                player.AssignedTeam.Value == Team.CorruptOfficials;

            bool isExecutingMove = 
                CurrentPhase.Value == GamePhase.Execution &&
                ExecIndex.Value >= 0 &&
                PlannedActions[ExecIndex.Value].ClientId == player.OwnerClientId &&
                PlannedActions[ExecIndex.Value].Action == ActionType.Move;

            string roundString = CurrentPhase.Value == GamePhase.Setup ? string.Empty : $"R: {GameRound.Value + 1}/{_totalGameRounds}";
            string phaseString = $"P: {CurrentPhase.Value}";
            string turnString = CurrentPhase.Value switch
            {
                GamePhase.Setup => string.Empty,
                GamePhase.Planning => $"T: {PlanningRound.Value + 1}/{_planningCycles}",
                GamePhase.Execution => $"T: {ExecIndex.Value / TurnOrder.Count + 1}/{_planningCycles}",
                _ => string.Empty
            };

            UIState state = new UIState()
            {
                ActionsVisible = CurrentPhase.Value == GamePhase.Planning && CurrentTurnClientId.Value == (long)player.OwnerClientId,
                NavigationVisible = isHidingItems || isExecutingMove,
                PreviewsVisible = !player.WearsMask.Value,
                WearActionText = player.WearsMask.Value ? "Unwear" : "Wear",
                WearActionVisible = player.AssignedTeam.Value == Team.Nabu && !player.IsDeanonimized.Value,
                WinTeam = WinTeam.Value,
                Reason = Reason.Value,
                TurnOrderUIShown = CurrentPhase.Value == GamePhase.Planning || CurrentPhase.Value == GamePhase.Execution,
                RoundPhaseTurnInfo = string.Join("; ", new string[] { roundString, phaseString, turnString })
            }; 

            _uiController.UpdateState(state);
        }
    }
}
