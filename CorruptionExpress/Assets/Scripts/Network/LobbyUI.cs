using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using SessionPlayer = Unity.Services.Multiplayer.IReadOnlyPlayer;

namespace Network
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Main Menu Panel")] [SerializeField]
        private GameObject mainMenuPanel;

        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_InputField lobbyNameInput;

        [Header("Lobby Panel")] [SerializeField]
        private GameObject lobbyPanel;

        [SerializeField] private TextMeshProUGUI lobbyCodeText;
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private TMP_FontAsset _playerListFontAsset;

        [Header("Status")] [SerializeField] private TextMeshProUGUI statusText;

        private GameNetworkManager _networkManager;
        private bool _isReady;
        private readonly List<GameObject> _playerListItems = new();

        private void Start()
        {
            _networkManager = GameNetworkManager.Instance;

            SetupButtonListeners();
            SubscribeToEvents();

            ShowMainMenu();
        }

        private void SetupButtonListeners()
        {
            createLobbyButton?.onClick.AddListener(OnCreateLobbyClicked);
            joinLobbyButton?.onClick.AddListener(OnJoinLobbyClicked);
            copyCodeButton?.onClick.AddListener(OnCopyCodeClicked);
            readyButton?.onClick.AddListener(OnReadyClicked);
            leaveButton?.onClick.AddListener(OnLeaveClicked);
            startGameButton?.onClick.AddListener(OnStartGameClicked);
        }

        private void SubscribeToEvents()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnServicesInitialized += OnServicesInitialized;
            _networkManager.OnError += OnError;
            _networkManager.OnSessionJoined += OnSessionJoined;
            _networkManager.OnSessionLeft += OnSessionLeft;
            _networkManager.OnDisconnected += OnDisconnected;
            _networkManager.OnPlayersChanged += OnPlayersChanged;

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnServicesInitialized -= OnServicesInitialized;
                _networkManager.OnError -= OnError;
                _networkManager.OnSessionJoined -= OnSessionJoined;
                _networkManager.OnSessionLeft -= OnSessionLeft;
                _networkManager.OnDisconnected -= OnDisconnected;
                _networkManager.OnPlayersChanged -= OnPlayersChanged;
            }

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnServicesInitialized()
        {
            SetStatus("Connected to services");
            SetLoading(false);
        }

        private void OnError(string error)
        {
            SetStatus($"Error: {error}");
            SetLoading(false);
        }

        private void OnSessionJoined()
        {
            UpdateStartButton();
        }

        private void OnSessionLeft()
        {
            ShowMainMenu();
            SetStatus("Left session");
        }

        private void OnDisconnected(string reason)
        {
            ShowMainMenu();
            SetStatus(reason);
        }

        private void OnPlayersChanged(IReadOnlyList<SessionPlayer> players)
        {
            UpdatePlayerList(players);
            UpdateStartButton();
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[LobbyUI] Client connected: {clientId}");
            UpdateStartButton();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[LobbyUI] Client disconnected: {clientId}");
            UpdateStartButton();
        }

        private async void OnCreateLobbyClicked()
        {
            var lobbyName = string.IsNullOrEmpty(lobbyNameInput?.text) ? "My Lobby" : lobbyNameInput.text;

            SetLoading(true);
            SetStatus("Creating session...");

            var sessionCode = await _networkManager.CreateSession(lobbyName);

            if (!string.IsNullOrEmpty(sessionCode))
            {
                ShowLobby(sessionCode, lobbyName);
                SetStatus("Session created! Share the code with friends.");
            }

            SetLoading(false);
        }

        private async void OnJoinLobbyClicked()
        {
            var joinCode = joinCodeInput?.text?.Trim().ToUpper();

            if (string.IsNullOrEmpty(joinCode))
            {
                SetStatus("Please enter a session code");
                return;
            }

            SetLoading(true);
            SetStatus("Joining session...");

            var success = await _networkManager.JoinSession(joinCode);
            if (success)
            {
                ShowLobby(_networkManager.CurrentSessionCode, "Game Session");
                SetStatus("Joined session!");
            }

            SetLoading(false);
        }

        private void OnCopyCodeClicked()
        {
            var code = _networkManager.CurrentSessionCode;
            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = code;
            SetStatus("Code copied to clipboard!");
        }

        private async void OnReadyClicked()
        {
            _isReady = !_isReady;
            UpdateReadyButton();

            await _networkManager.SetPlayerProperty("IsReady", _isReady.ToString().ToLower());
        }

        private async void OnLeaveClicked()
        {
            await _networkManager.LeaveSession();
            _isReady = false;
            UpdateReadyButton();
        }

        private void OnStartGameClicked()
        {
            if (_networkManager.IsHost && _networkManager.AreAllPlayersReady())
            {
                _networkManager.StartGame();
            }
        }

        private void ShowMainMenu()
        {
            mainMenuPanel?.SetActive(true);
            lobbyPanel?.SetActive(false);
            _isReady = false;
            UpdateReadyButton();
        }

        private void ShowLobby(string lobbyCode, string lobbyName)
        {
            mainMenuPanel?.SetActive(false);
            lobbyPanel?.SetActive(true);

            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = lobbyCode;
            }

            if (lobbyNameText != null)
            {
                lobbyNameText.text = lobbyName;
            }

            UpdateStartButton();
        }

        private void UpdatePlayerList(IReadOnlyList<SessionPlayer> players)
        {
            // Clear existing items
            foreach (var item in _playerListItems)
            {
                Destroy(item);
            }

            _playerListItems.Clear();

            if (playerListItemPrefab == null || playerListContainer == null || players == null)
            {
                return;
            }

            // Create new items
            var index = 0;
            foreach (var player in players)
            {
                var item = Instantiate(playerListItemPrefab, playerListContainer);
                _playerListItems.Add(item);

                var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText == null)
                {
                    nameText = item.AddComponent<TextMeshProUGUI>();
                    nameText.font = _playerListFontAsset;
                }
                
                Debug.Log($"[LobbyUI] Player: {player.Id}. Properties: {string.Join(" | ", player.Properties.Keys.ToList())}");

                var isReady = player.Properties != null &&
                              player.Properties.TryGetValue("IsReady", out var readyProperty) &&
                              readyProperty.Value == "true";

                var hostLabel = index == 0 ? " [Host]" : "";
                var readyLabel = isReady ? " [Ready]" : "";

                nameText.text = $"Player {index + 1}{hostLabel}{readyLabel}";

                index++;
            }
        }

        private void UpdateReadyButton()
        {
            if (readyButton == null)
            {
                return;
            }

            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = _isReady ? "Not Ready" : "Ready";
        }

        private void UpdateStartButton()
        {
            if (startGameButton == null)
            {
                return;
            }

            var canStart = _networkManager != null &&
                           _networkManager.IsHost &&
                           _networkManager.AreAllPlayersReady();

            startGameButton.interactable = canStart;
            startGameButton.gameObject.SetActive(_networkManager != null && _networkManager.IsHost);
        }

        private void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }

            Debug.Log($"[LobbyUI] {status}");
        }

        private void SetLoading(bool loading)
        {
            if (createLobbyButton != null)
            {
                createLobbyButton.interactable = !loading;
            }

            if (joinLobbyButton != null)
            {
                joinLobbyButton.interactable = !loading;
            }
        }
    }
}