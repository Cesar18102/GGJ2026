using System.Collections.Generic;
using GameState;
using Teams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Team Reveal Panel")]
        [SerializeField] private GameObject teamRevealPanel;
        [SerializeField] private Image teamRevealBackground;
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI teamDescriptionText;

        [Header("Nabu Waiting Panel")]
        [SerializeField] private GameObject nabuWaitingPanel;
        [SerializeField] private TextMeshProUGUI waitingMessageText;

        [Header("Team Indicator")]
        [SerializeField] private GameObject teamIndicator;
        [SerializeField] private TextMeshProUGUI teamLabelText;

        [Header("Team Colors")]
        [SerializeField] private Color corruptOfficialsColor = new(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color nabuColor = new(0.2f, 0.4f, 0.8f);

        [Header("Team Descriptions")]
        [SerializeField] private string corruptOfficialsDescription = "You are a Corrupt Official.\nHide the evidence and avoid detection!";
        [SerializeField] private string nabuDescription = "You are NABU.\nFind the incriminating evidence and expose corruption!";
        [SerializeField] private string nabuWaitingMessage = "You are on your way to find incriminating evidence.\nPlease wait...";

        private Team _localPlayerTeam = Team.Unassigned;

        private void Start()
        {
            HideAllPanels();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                HandlePhaseChanged(GameStateManager.Instance.CurrentPhase.Value);
            }

            var localPlayer = PlayerTeamController.GetLocalPlayer();
            if (localPlayer != null)
            {
                localPlayer.OnTeamAssigned += HandleLocalTeamAssigned;

                if (localPlayer.AssignedTeam.Value != Team.Unassigned)
                {
                    HandleLocalTeamAssigned(localPlayer.AssignedTeam.Value);
                }
            }
            else
            {
                StartCoroutine(WaitForLocalPlayer());
            }
        }

        private System.Collections.IEnumerator WaitForLocalPlayer()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);

                var localPlayer = PlayerTeamController.GetLocalPlayer();
                if (localPlayer != null)
                {
                    localPlayer.OnTeamAssigned += HandleLocalTeamAssigned;

                    if (localPlayer.AssignedTeam.Value != Team.Unassigned)
                    {
                        HandleLocalTeamAssigned(localPlayer.AssignedTeam.Value);
                    }

                    yield break;
                }
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }

            var localPlayer = PlayerTeamController.GetLocalPlayer();
            if (localPlayer != null)
            {
                localPlayer.OnTeamAssigned -= HandleLocalTeamAssigned;
            }
        }

        private void HandleLocalTeamAssigned(Team team)
        {
            _localPlayerTeam = team;
            UpdateTeamIndicator();

            if (GameStateManager.Instance != null)
            {
                HandlePhaseChanged(GameStateManager.Instance.CurrentPhase.Value);
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            HideAllPanels();

            switch (phase)
            {
                case GamePhase.WaitingForAssignment:
                    break;

                case GamePhase.TeamReveal:
                    ShowTeamReveal();
                    break;

                case GamePhase.NabuWaiting:
                    if (_localPlayerTeam == Team.Nabu)
                    {
                        ShowNabuWaiting();
                    }
                    ShowTeamIndicator();
                    break;

                case GamePhase.MainGameplay:
                    ShowTeamIndicator();
                    break;
            }
        }

        private void HideAllPanels()
        {
            if (teamRevealPanel != null) teamRevealPanel.SetActive(false);
            if (nabuWaitingPanel != null) nabuWaitingPanel.SetActive(false);
            if (teamIndicator != null) teamIndicator.SetActive(false);
        }

        private void ShowTeamReveal()
        {
            if (teamRevealPanel == null || _localPlayerTeam == Team.Unassigned)
            {
                return;
            }

            teamRevealPanel.SetActive(true);

            var isCorrupt = _localPlayerTeam == Team.CorruptOfficials;
            var teamColor = isCorrupt ? corruptOfficialsColor : nabuColor;

            if (teamRevealBackground != null)
            {
                teamRevealBackground.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.9f);
            }

            if (teamNameText != null)
            {
                teamNameText.text = isCorrupt ? "CORRUPT OFFICIAL" : "NABU AGENT";
                teamNameText.color = Color.white;
            }

            if (teamDescriptionText != null)
            {
                teamDescriptionText.text = isCorrupt ? corruptOfficialsDescription : nabuDescription;
            }
        }

        private void ShowNabuWaiting()
        {
            if (nabuWaitingPanel == null)
            {
                return;
            }

            nabuWaitingPanel.SetActive(true);

            if (waitingMessageText != null)
            {
                waitingMessageText.text = nabuWaitingMessage;
            }
        }

        private void ShowTeamIndicator()
        {
            if (teamIndicator == null || _localPlayerTeam == Team.Unassigned)
            {
                return;
            }

            teamIndicator.SetActive(true);
            UpdateTeamIndicator();
        }

        private void UpdateTeamIndicator()
        {
            if (teamLabelText == null || _localPlayerTeam == Team.Unassigned)
            {
                return;
            }

            var isCorrupt = _localPlayerTeam == Team.CorruptOfficials;
            teamLabelText.text = isCorrupt ? "Corrupt Official" : "NABU";
            teamLabelText.color = isCorrupt ? corruptOfficialsColor : nabuColor;
        }
    }
}
