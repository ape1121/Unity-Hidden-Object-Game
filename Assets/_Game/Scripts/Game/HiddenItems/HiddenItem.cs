using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HiddenItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ItemData itemData;
    [SerializeField] private bool collected;

    public ItemData ItemData => itemData;
    public string ItemId => itemData.Id;
    public bool IsCollected => collected;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    public void Initialize(ItemData data)
    {
        itemData = data;
        collected = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.Icon;
        }

        if (!string.IsNullOrWhiteSpace(data.Id))
        {
            gameObject.name = "HiddenItem_" + data.Id;
        }
    }

    public bool TryMarkCollected()
    {
        if (collected)
        {
            return false;
        }

        collected = true;
        return true;
    }

    public void ResetCollectedState()
    {
        collected = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
#endif
}
