using UnityEngine;

[ExecuteAlways]
public class LevelItemMarker : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private int sortingOrder;
    [SerializeField] private SpriteRenderer iconRenderer;

    public string ItemId
    {
        get => itemId;
        set => itemId = value;
    }

    public void ApplyPlacement(LevelItemPlacement placement)
    {
        EnsureRendererReference();

        itemId = placement.ItemId;
        sortingOrder = placement.SortingOrder;

        transform.position = placement.Position;
        transform.localScale = placement.Scale == Vector3.zero ? Vector3.one : placement.Scale;
        transform.rotation = Quaternion.Euler(0f, 0f, placement.RotationZ);

        if (iconRenderer != null)
        {
            iconRenderer.sortingOrder = sortingOrder;
        }

        UpdateName();
    }

    public LevelItemPlacement ToPlacement()
    {
        return new LevelItemPlacement
        {
            ItemId = itemId,
            Position = transform.position,
            Scale = transform.localScale,
            RotationZ = transform.eulerAngles.z,
            SortingOrder = sortingOrder
        };
    }

    public void RefreshIcon(AllItems allItems)
    {
        EnsureRendererReference();

        if (iconRenderer == null)
        {
            return;
        }

        iconRenderer.sortingOrder = sortingOrder;

        ItemData itemData;
        if (allItems != null && allItems.TryGetById(itemId, out itemData))
        {
            iconRenderer.sprite = itemData.Icon;
            iconRenderer.enabled = itemData.Icon != null;
        }
        else
        {
            iconRenderer.sprite = null;
            iconRenderer.enabled = false;
        }

        UpdateName();
    }

    private void UpdateName()
    {
        gameObject.name = string.IsNullOrWhiteSpace(itemId) ? "ItemMarker_Empty" : "ItemMarker_" + itemId;
    }

    private void EnsureRendererReference()
    {
        if (iconRenderer == null)
        {
            iconRenderer = GetComponent<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (iconRenderer == null)
        {
            EnsureRendererReference();
        }

        UpdateName();
    }
#endif
}
