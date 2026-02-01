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

    public void Set(Sprite sprite, bool highlight)
    {
        _icon.sprite = sprite;

        if (_highlight != null)
        {
            _highlight.color = highlight ? _highlightColor : _noHighlightColor;
        }
    }
}
