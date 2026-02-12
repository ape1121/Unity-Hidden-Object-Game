using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllItems", menuName = "SearchIt/AllItems", order = 1)]
public class AllItems : ScriptableObject
{
    [SerializeField] private ItemData[] items;

    private Dictionary<string, ItemData> itemsById;

    public IReadOnlyList<ItemData> Items => items ?? Array.Empty<ItemData>();

    public bool Contains(string itemId)
    {
        ItemData _;
        return TryGetById(itemId, out _);
    }

    public bool TryGetById(string itemId, out ItemData itemData)
    {
        itemData = default;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        EnsureLookup();
        return itemsById.TryGetValue(itemId, out itemData);
    }

    private void EnsureLookup()
    {
        if (itemsById == null)
        {
            RebuildLookup();
        }
    }

    private void OnEnable()
    {
        RebuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildLookup();
    }
#endif

    private void RebuildLookup()
    {
        if (itemsById == null)
        {
            itemsById = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        }
        else
        {
            itemsById.Clear();
        }

        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            ItemData item = items[i];
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            // Keep first entry stable if duplicates exist.
            if (!itemsById.ContainsKey(item.Id))
            {
                itemsById.Add(item.Id, item);
            }
        }
    }
}
