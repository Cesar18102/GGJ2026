using GameState;
using System.Collections.Generic;
using System.Linq;
using Teams;
using TMPro;
using UnityEngine;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private TurnIndicator _iconPrefab;
    [SerializeField] private TMP_Text _phaseNameText;

    [Header("Fallback sprites")]
    [SerializeField] private Sprite _nabuSprite;
    [SerializeField] private Sprite _corruptSprite;

    private readonly Dictionary<ulong, PlayerTeamController> _byId = new();
    private readonly List<TurnIndicator> _icons = new();

    private void Start()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        CachePlayers();
        Rebuild();

        gsm.OnPhaseChanged += Gsm_OnPhaseChanged;
        gsm.TurnOrder.OnListChanged += _ => Rebuild();
        gsm.CurrentTurnClientId.OnValueChanged += (_, __) => RefreshHighlights();
        gsm.LastActionByClientId.OnValueChanged += (_, __) => RefreshHighlights();
        gsm.LastPlannedAction.OnValueChanged += (_, __) => RefreshHighlights();
        gsm.PlannedActions.OnListChanged += _ => RefreshHighlights();
        gsm.ExecIndex.OnValueChanged += (_, __) => RefreshHighlights();
    }

    private void Gsm_OnPhaseChanged(GamePhase obj)
    {
        _phaseNameText.text = $"{obj} Phase";
    }

    private void CachePlayers()
    {
        _byId.Clear();
        foreach (var p in PlayerTeamController.Select(player => player))
            _byId[p.OwnerClientId] = p.GetComponent<PlayerTeamController>();
    }

    private void Rebuild()
    {
        for (int i = _container.childCount - 1; i >= 0; i--)
            Destroy(_container.GetChild(i).gameObject);

        _icons.Clear();

        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        foreach (var clientId in gsm.TurnOrder)
        {
            var icon = Instantiate(_iconPrefab, _container);
            _icons.Add(icon);

            var sprite = ResolveSprite(clientId);
            bool isCurrent = clientId == gsm.CurrentTurnClientId.Value;
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

        ulong current = gsm.CurrentPhase.Value == GamePhase.Planning ?
            gsm.CurrentTurnClientId.Value : gsm.CurrentExecutedByClientId.Value;

        for (int i = 0; i < gsm.TurnOrder.Count && i < _icons.Count; i++)
        {
            ulong id = gsm.TurnOrder[i];
            var sprite = ResolveSprite(id);

            string text = string.Empty;
            
            if (id == gsm.CurrentExecutedByClientId.Value && gsm.CurrentPhase.Value == GamePhase.Execution)
            {
                text = $"Current Action: {gsm.PlannedActions[gsm.ExecIndex.Value].Action.ToString()}";
            }
            else if (id == gsm.LastActionByClientId.Value && gsm.CurrentPhase.Value == GamePhase.Planning)
            {
                text = $"Last Action: {gsm.CurrentExecutedAction.Value.ToString()}";
            }

            _icons[i].Set(sprite, id == current, text);
        }
    }

    private Sprite ResolveSprite(ulong clientId)
    {
        if (_byId.TryGetValue(clientId, out var ptc))
        {
            return ptc.AssignedTeam.Value == Team.Nabu ? _nabuSprite : _corruptSprite;
        }

        return _corruptSprite;
    }
}
