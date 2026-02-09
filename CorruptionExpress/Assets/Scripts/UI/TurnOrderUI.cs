using Assets.Scripts.Helpers;
using GameState;
using System.Collections.Generic;
using Teams;
using TMPro;
using UnityEngine;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiHolder;
    [SerializeField] private Transform _container;
    [SerializeField] private TurnIndicator _iconPrefab;
    [SerializeField] private TMP_Text _phaseNameText;

    [Header("Fallback sprites")]
    [SerializeField] private Sprite _nabuSprite;
    [SerializeField] private Sprite _corruptSprite;

    private readonly List<TurnIndicator> _icons = new();

    private void Start()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        Rebuild();

        gsm.OnPhaseChanged += OnPhaseChanged;
        gsm.TurnOrder.OnListChanged += _ => Rebuild();

        gsm.CurrentTurnClientId.OnValueChanged += (_, __) => RefreshHighlights();
        gsm.PlannedActions.OnListChanged += _ => RefreshHighlights();
        gsm.ExecIndex.OnValueChanged += (_, __) => RefreshHighlights();
    }

    private void OnPhaseChanged(GamePhase obj)
    {
        _uiHolder.SetActive(obj == GamePhase.Planning || obj == GamePhase.Execution);
        _phaseNameText.text = $"{obj} Phase";
    }

    private void Rebuild()
    {
        for (int i = _container.childCount - 1; i >= 0; i--)
        {
            Destroy(_container.GetChild(i).gameObject);
        }

        _icons.Clear();

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        foreach (ulong clientId in gsm.TurnOrder)
        {
            TurnIndicator icon = Instantiate(_iconPrefab, _container);
            _icons.Add(icon);

            Sprite sprite = ResolveSprite(clientId);
            bool isCurrent = (long)clientId == gsm.CurrentTurnClientId.Value;
            ActionType action = gsm.LastPlannedAction.Value;

            icon.Set(sprite, isCurrent, string.Empty);
        }
    }

    private void RefreshHighlights()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) {
            return;
        }

        GamePhase currentPhase = gsm.CurrentPhase.Value;

        for (int i = 0; i < gsm.TurnOrder.Count && i < _icons.Count; i++)
        {
            ulong id = gsm.TurnOrder[i];
            long lid = (long)id;
            Sprite sprite = ResolveSprite(id);

            bool highlight = false;
            string text = string.Empty;

            if (currentPhase == GamePhase.Planning)
            {
                highlight = lid == gsm.CurrentTurnClientId.Value;

                if (gsm.PlannedActions.Count > 0)
                {
                    PlannedAction lastPlannedAction = gsm.PlannedActions[gsm.PlannedActions.Count - 1];
                    text = lid == (long)lastPlannedAction.ClientId ? $"Last: {lastPlannedAction.Action.ToString()}" : string.Empty;
                }
            }
            else if (currentPhase == GamePhase.Execution && gsm.ExecIndex.Value >= 0 && gsm.ExecIndex.Value < gsm.PlannedActions.Count)
            {
                PlannedAction action = gsm.PlannedActions[gsm.ExecIndex.Value];

                highlight = lid == (long)action.ClientId;
                text = highlight ? $"Current: {action.Action.ToString()}" : string.Empty;
            }

            _icons[i].Set(sprite, highlight, text);
        }
    }

    private Sprite ResolveSprite(ulong clientId)
    {
        return PlayersHelper.GetPlayer(clientId).AssignedTeam.Value == Team.Nabu ? _nabuSprite : _corruptSprite;
    }
}
