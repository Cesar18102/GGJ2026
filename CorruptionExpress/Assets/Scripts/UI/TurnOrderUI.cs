using GameState;
using System.Collections.Generic;
using System.Linq;
using Teams;
using UnityEngine;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private TurnIndicator _iconPrefab;

    [Header("Fallback sprites")]
    [SerializeField] private Sprite _nabuSprite;
    [SerializeField] private Sprite _corruptSprite;

    private readonly List<TurnIndicator> _icons = new();

    private void Start()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        Rebuild();

        gsm.TurnOrder.OnListChanged += _ => Rebuild();
        gsm.CurrentTurnClientId.OnValueChanged += (_, __) => RefreshHighlights();
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

            icon.Set(sprite, isCurrent);
        }
    }

    private void RefreshHighlights()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        for (int i = 0; i < gsm.TurnOrder.Count && i < _icons.Count; i++)
        {
            ulong id = gsm.TurnOrder[i];
            var sprite = ResolveSprite(id);
            _icons[i].Set(sprite, id == gsm.CurrentTurnClientId.Value);
        }
    }

    private Sprite ResolveSprite(ulong clientId)
    {
        var playerById = PlayerTeamController.Select(player => player).ToDictionary(player => player.OwnerClientId);
        return playerById[clientId].GetComponent<PlayerTeamController>().AssignedTeam.Value == Team.Nabu ? _nabuSprite : _corruptSprite;
    }
}
