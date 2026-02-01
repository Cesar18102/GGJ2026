using Assets.Scripts.Helpers;
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
        private Transform[] spawnPoints;

        [SerializeField]
        private int _planningCycles = 3;

        [SerializeField]
        private int _totalGameRounds = 5;

        [SerializeField]
        private SceneInputController _sceneInputController;

        [SerializeField]
        private GoToSpotActionHandler _goToSpotActionHandler;

        [SerializeField]
        private PutEvidenceActionHandler _putEvidenceActionHandler;

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

        private readonly Queue<(ulong clientId, ActionType action)> _actionsQueue = new();

        private int _turnIndex;
        

        public NetworkVariable<GamePhase> CurrentPhase { get; } = new(
            GamePhase.WaitingForAssignment,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public event Action<GamePhase> OnPhaseChanged;

        public NetworkVariable<int> RemainingEvidences { get; } = new NetworkVariable<int>(
            -1, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );


        private Coroutine _phaseTimerCoroutine;

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

            CurrentPhase.OnValueChanged += HandlePhaseChanged;
            RemainingEvidences.OnValueChanged += HandleRemainingNumberOfItemsChanged;
            _uiController.OnAction += _uiController_OnAction;

            if (IsServer)
            {
                StartGameFlow();
            }

            Debug.Log($"[GameStateManager] Network spawned. Current phase: {CurrentPhase.Value}");
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"[GameStateManager] Network despawned.");

            CurrentPhase.OnValueChanged -= HandlePhaseChanged;
            RemainingEvidences.OnValueChanged -= HandleRemainingNumberOfItemsChanged;
            _uiController.OnAction -= _uiController_OnAction;

            TurnOrder.Dispose();

            if (_phaseTimerCoroutine != null)
            {
                StopCoroutine(_phaseTimerCoroutine);
            }

            if (Instance == this)
            {
                Instance = null;
            }

            base.OnNetworkDespawn();
        }

        private void StartGameFlow()
        {
            if (!IsServer)
            {
                return;
            }

            StartCoroutine(GameFlowCoroutine());
        }

        private IEnumerator GameFlowCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            if (TeamManager.Instance != null)
            {
                TeamManager.Instance.AssignTeams();
                RemainingEvidences.Value = _numberOfEvidences * TeamManager.Instance.GetCountCorruption();
            }
            else
            {
                Debug.LogError("[GameStateManager] TeamManager not found!");
                yield break;
            }

            PlayerTeamController.ForEachPlayer(player => player.transform.SetPositionAndRotation(Vector3.up * 100, Quaternion.identity));

            yield return new WaitForSeconds(0.5f);
            SetPhase(GamePhase.Setup);

            //setup phase
            SetTeamActive(Team.CorruptOfficials, true);
            SetTeamActive(Team.Nabu, false);

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().AddEvidence(_numberOfEvidences));
            Debug.Log("[SETUP] Corrupt evidence counts: " + string.Join(", ", PlayerTeamController.Select(Team.CorruptOfficials, p => p.GetComponent<PlayerNetState>().EvidenceCount.Value)));

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged += PlayerItemsCountChanged);

            yield return new WaitUntil(() => PlayerTeamController.Select(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.Value == 0).All(value => value));

            PlayerTeamController.ForEachPlayer(Team.CorruptOfficials, player => player.GetComponent<PlayerNetState>().EvidenceCount.OnValueChanged -= PlayerItemsCountChanged);

            SetTeamActive(Team.Nabu, true);
            SpawnPlayersRandom();

            DefineOrder();

            for (int i = 0; i < _totalGameRounds; ++i)
            {
                Debug.Log($"Game Round: {i + 1}");

                _actionsQueue.Clear();
                PlanningRound.Value = 0;
                _turnIndex = 0;

                CurrentTurnClientId.Value = TurnOrder[_turnIndex];
                LastPlannedAction.Value = ActionType.None;
                LastActionByClientId.Value = 0;

                SetPhase(GamePhase.Planning);

                yield return new WaitUntil(() => PlanningRound.Value >= _planningCycles);

                //ExecutionRound.Value = 0;
                //_turnIndex = 0;
                //CurrentTurnClientId.Value = TurnOrder[_turnIndex];

                //SetPhase(GamePhase.Execution);
                //ExecutionLoop();

                //yield return new WaitUntil(() => ExecutionRound.Value >= _planningCycles);
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

        private void ExecutionLoop()
        {

        }

        public void ForcePhase(GamePhase phase)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[GameStateManager] Only server can force phase changes");
                return;
            }

            if (_phaseTimerCoroutine != null)
            {
                StopCoroutine(_phaseTimerCoroutine);
                _phaseTimerCoroutine = null;
            }

            SetPhase(phase);
        }

        #region Gameplay


        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            Debug.Log($"[GameStateManager] Phase changed from {previousPhase} to {newPhase}");
            OnPhaseChanged?.Invoke(newPhase);

            switch (newPhase)
            {
                case GamePhase.Setup:
                    HandleSetupPhase();
                    break;
            }
        }

        private void HandleSetupPhase()
        {
            PlayerTeamController currentPlayer = PlayerTeamController.GetLocalPlayer();
            _uiController.ShowPreparePanel(currentPlayer.AssignedTeam.Value);

            if (currentPlayer.AssignedTeam.Value == Team.CorruptOfficials)
            {
                _sceneInputController.SetCurrentGameActionHandler(_putEvidenceActionHandler);
            }
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

        private void SpawnPlayersRandom()
        {
            Debug.Log($"Players are being spawed");

            // тасую точки
            spawnPoints.Shuffle();

            PlayerTeamController.ForEachPlayer((player, i) =>
            {
                var p = spawnPoints[i % spawnPoints.Length];
                player.transform.SetPositionAndRotation(p.position, p.rotation);

                // якщо треба для 2D: обнулити Z
                var pos = player.transform.position;
                pos.z = 0f;
                player.transform.position = pos;
            });
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

        private void _uiController_OnAction(object sender, ActionType e)
        {
            Debug.Log($"[UI_SEND] local={NetworkManager.Singleton.LocalClientId} current(localView)={CurrentTurnClientId.Value} phase={CurrentPhase.Value}");

            TryAddPlannedActionServerRpc(e);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void TryAddPlannedActionServerRpc(ActionType action, RpcParams rpcParams = default)
        {
            Debug.Log($"RPC action {action} sender={rpcParams.Receive.SenderClientId} current={CurrentTurnClientId.Value}; local={NetworkManager.Singleton.LocalClientId}");


            Debug.Log($"Trying to push {action}");
            if (CurrentPhase.Value != GamePhase.Planning)
            {
                return;
            }

            ulong sender = rpcParams.Receive.SenderClientId;

            if (sender != CurrentTurnClientId.Value)
            {
                return;
            }

            Debug.Log($"Pushing {action}");
            _actionsQueue.Enqueue((sender, action));

            LastPlannedAction.Value = action;
            LastActionByClientId.Value = sender;

            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            Debug.Log($"[TURN] next={CurrentTurnClientId.Value} idx={_turnIndex} round={PlanningRound.Value}");


            Debug.Log($"Current turn: {_turnIndex}");
            _turnIndex++;
            Debug.Log($"New turn: {_turnIndex}");

            if (_turnIndex >= TurnOrder.Count)
            {
                Debug.Log($"Planning round: {PlanningRound.Value} completed.");

                _turnIndex = 0;
                PlanningRound.Value++;

                if (PlanningRound.Value >= _planningCycles)
                {
                    Debug.Log("[Planning] Done. Queue size=" + _actionsQueue.Count);
                    return;
                }
            }

            CurrentTurnClientId.Value = TurnOrder[_turnIndex];
        }

        #endregion Gameplay
    }
}
