using System;
using System.Collections;
using Teams;
using Unity.Netcode;
using UnityEngine;

namespace GameState
{
    public class GameStateManager : NetworkBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Phase Timings")]
        [SerializeField] private float teamRevealDuration = 5f;
        [SerializeField] private float nabuWaitingDuration = 15f;

        public NetworkVariable<GamePhase> CurrentPhase { get; } = new(
            GamePhase.WaitingForAssignment,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action<GamePhase> OnPhaseChanged;

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

            if (IsServer)
            {
                StartGameFlow();
            }

            Debug.Log($"[GameStateManager] Network spawned. Current phase: {CurrentPhase.Value}");
        }

        public override void OnNetworkDespawn()
        {
            CurrentPhase.OnValueChanged -= HandlePhaseChanged;

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
            }
            else
            {
                Debug.LogError("[GameStateManager] TeamManager not found!");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            SetPhase(GamePhase.TeamReveal);
            yield return new WaitForSeconds(teamRevealDuration);

            SetPhase(GamePhase.NabuWaiting);
            yield return new WaitForSeconds(nabuWaitingDuration);

            SetPhase(GamePhase.MainGameplay);
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
    }
}
