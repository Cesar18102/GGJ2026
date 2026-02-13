using UnityEngine;

public class NabuAppearanceController : MonoBehaviour
{
    [SerializeField]
    private Sprite _maskSprite;

    [SerializeField]
    private Sprite _noMaskSprite;

    [SerializeField]
    private SpriteRenderer _headRenderer;

    [SerializeField]
    private GameObject _head;

    public void WearMask()
    {
        _headRenderer.sprite = _maskSprite;
    }

    public void TakeMaskOff()
    {
        _headRenderer.sprite = _noMaskSprite;
    }

    public void Deanonimize()
    {
        //Doesn't work. Currently deanon is indicated by non-masked face of agent in turns order UI.
        _head.transform.Rotate(0, 0, 35);
    }
}
