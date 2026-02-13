using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private Image _icon;

    [SerializeField]
    private Image _highlight;

    [SerializeField]
    private Color _highlightColor;

    [SerializeField]
    private Color _noHighlightColor;

    [SerializeField]
    private TMP_Text _textHolder;

    [SerializeField]
    private GameObject _actionContainer;

    [SerializeField]
    private Sprite _wearSprite;

    [SerializeField]
    private Sprite _moveSprite;

    [SerializeField]
    private Sprite _searchSprite;

    [SerializeField]
    private Sprite _putSprite;

    public void Set(Sprite sprite, bool highlight, string text, ActionType? action)
    {
        _icon.sprite = sprite;
        _icon.color = highlight ? _highlightColor : _noHighlightColor;

        if (_textHolder != null)
        {
            _textHolder.text = text;
        }

        if (_highlight != null)
        {
            _highlight.color = highlight ? _highlightColor : _noHighlightColor;
        }

        _actionContainer.SetActive(action.HasValue);
        Sprite actionSprite = action switch
        {
            ActionType.Wear => _wearSprite,
            ActionType.Move => _moveSprite,
            ActionType.Search => _searchSprite,
            ActionType.Put => _putSprite,
            _ => null
        };
        _actionContainer.GetComponent<Image>().sprite = actionSprite;
    }
}
