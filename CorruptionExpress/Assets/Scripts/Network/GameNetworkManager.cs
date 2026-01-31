using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using SessionPlayer = Unity.Services.Multiplayer.IReadOnlyPlayer;

namespace Network
{
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Header("Configuration")] [SerializeField]
        private string gameSceneName = "GameScene";

        [SerializeField] private int maxPlayers = 4;

        public event Action OnServicesInitialized;
        public event Action<string> OnError;
        public event Action OnSessionJoined;
        public event Action OnSessionLeft;
        public event Action<IReadOnlyList<SessionPlayer>> OnPlayersChanged;

        public bool IsInitialized { get; private set; }
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
        public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        public string CurrentSessionCode => _currentSession?.Code;
        public bool IsSessionHost => _currentSession != null && _currentSession.IsHost;

        private ISession _currentSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await InitializeServices();
        }

        private async Task InitializeServices()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsInitialized = true;
                Debug.Log(
                    $"[GameNetworkManager] Services initialized. Player ID: {AuthenticationService.Instance.PlayerId}");
                OnServicesInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameNetworkManager] Failed to initialize services: {e.Message}");
                OnError?.Invoke($"Failed to initialize services: {e.Message}");
            }
        }

        public async Task<string> CreateSession(string sessionName = null)
        {
            if (!IsInitialized)
            {
                OnError?.Invoke("Services not initialized");
                return null;
            }

            try
            {
                var options = new SessionOptions
                {
                    Name = sessionName ?? "Game Session",
                    MaxPlayers = maxPlayers
                }.WithRelayNetwork();

                _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

                _currentSession.PlayerJoined += OnPlayerJoined;
                _currentSession.PlayerLeaving += OnPlayerLeft;
                _currentSession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;

                Debug.Log($"[GameNetworkManager] Created session. Code: {_currentSession.Code}");
                OnSessionJoined?.Invoke();
                OnPlayersChanged?.Invoke(_currentSession.Players);

                return _currentSession.Code;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameNetworkManager] Failed to create session: {e.Message}");
                OnError?.Invoke($"Failed to create session: {e.Message}");
                return null;
            }
        }

        public async Task<bool> JoinSession(string sessionCode)
        {
            if (!IsInitialized)
            {
                OnError?.Invoke("Services not initialized");
                return false;
            }

            try
            {
                _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);

                _currentSession.PlayerJoined += OnPlayerJoined;
                _currentSession.PlayerLeaving += OnPlayerLeft;
                _currentSession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;

                Debug.Log($"[GameNetworkManager] Joined session. Code: {_currentSession.Code}");
                OnSessionJoined?.Invoke();
                OnPlayersChanged?.Invoke(_currentSession.Players);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameNetworkManager] Failed to join session: {e.Message}");
                OnError?.Invoke($"Failed to join session: {e.Message}");
                return false;
            }
        }

        public async Task LeaveSession()
        {
            if (_currentSession == null)
            {
                return;
            }

            try
            {
                _currentSession.PlayerJoined -= OnPlayerJoined;
                _currentSession.PlayerLeaving -= OnPlayerLeft;

                await _currentSession.LeaveAsync();
                Debug.Log("[GameNetworkManager] Left session");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameNetworkManager] Failed to leave session: {e.Message}");
            }
            finally
            {
                _currentSession = null;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                OnSessionLeft?.Invoke();
            }
        }

        public void StartGame()
        {
            if (!IsHost)
            {
                Debug.LogWarning("[GameNetworkManager] Only host can start the game");
                return;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        public IReadOnlyList<SessionPlayer> GetPlayers()
        {
            return _currentSession?.Players;
        }

        public async Task SetPlayerProperty(string key, string value)
        {
            if (_currentSession == null)
            {
                return;
            }

            try
            {
                _currentSession.CurrentPlayer.SetProperty(key, new PlayerProperty(value));
                await _currentSession.SaveCurrentPlayerDataAsync();

                Debug.Log($"[GameNetworkManager] Set player property: {key} = {value}");

                OnPlayersChanged?.Invoke(_currentSession.Players);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameNetworkManager] Failed to set player property: {e.Message}");
            }
        }

        public bool AreAllPlayersReady()
        {
            if (_currentSession == null || _currentSession.Players.Count < 2)
            {
                return false;
            }

            foreach (var player in _currentSession.Players)
            {
                if (player.Properties == null ||
                    !player.Properties.TryGetValue("IsReady", out var readyProperty) ||
                    readyProperty.Value != "true")
                {
                    return false;
                }
            }

            return true;
        }

        private void OnPlayerJoined(string playerId)
        {
            Debug.Log($"[GameNetworkManager] Player joined: {playerId}");

            _ = UpdatePlayers();
        }

        private void OnPlayerLeft(string playerId)
        {
            Debug.Log($"[GameNetworkManager] Player left: {playerId}");

            _ = UpdatePlayers();
        }

        private void OnPlayerPropertiesChanged()
        {
            Debug.Log("[GameNetworkManager] Player properties changed");

            _ = UpdatePlayers();
        }

        private async Task UpdatePlayers()
        {
            if (_currentSession == null)
            {
                return;
            }

            await _currentSession.RefreshAsync();

            OnPlayersChanged?.Invoke(_currentSession.Players);
        }


        private void OnDestroy()
        {
            if (_currentSession != null)
            {
                _currentSession.PlayerJoined -= OnPlayerJoined;
                _currentSession.PlayerLeaving -= OnPlayerLeft;
                _currentSession.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}