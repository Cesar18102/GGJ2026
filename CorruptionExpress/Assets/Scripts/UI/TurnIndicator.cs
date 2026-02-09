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

    public void Set(Sprite sprite, bool highlight, string text)
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
    }
}
