using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ItemSlotController : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetItem(ItemController item)
    {
        _spriteRenderer.sprite = item.GetComponent<SpriteRenderer>().sprite;
    }

    public void UnsetItem()
    {
        _spriteRenderer.sprite = null;
    }
}

