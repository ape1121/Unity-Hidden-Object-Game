using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemainingItems : MonoBehaviour
{
    [SerializeField] private RemainingItem itemPrefab;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private bool sortByName = true;

    private readonly List<RemainingItem> itemViews = new List<RemainingItem>();
    private readonly Dictionary<string, RemainingEntry> entriesById = new Dictionary<string, RemainingEntry>(StringComparer.Ordinal);

    public void BuildFromSpawnedItems(IReadOnlyList<HiddenItem> spawnedItems)
    {
        ClearAll();

        if (itemPrefab == null)
        {
            Debug.LogWarning("RemainingItems: itemPrefab is not assigned.");
            return;
        }

        if (spawnedItems == null)
        {
            return;
        }

        var aggregatedEntries = new List<RemainingEntryData>();
        var indexById = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            HiddenItem hiddenItem = spawnedItems[i];
            if (hiddenItem == null || hiddenItem.IsCollected)
            {
                continue;
            }

            ItemData itemData = hiddenItem.ItemData;
            if (string.IsNullOrWhiteSpace(itemData.Id))
            {
                continue;
            }

            int existingIndex;
            if (!indexById.TryGetValue(itemData.Id, out existingIndex))
            {
                existingIndex = aggregatedEntries.Count;
                indexById[itemData.Id] = existingIndex;
                aggregatedEntries.Add(new RemainingEntryData
                {
                    ItemData = itemData,
                    Count = 0
                });
            }

            RemainingEntryData updatedEntry = aggregatedEntries[existingIndex];
            updatedEntry.Count += 1;
            aggregatedEntries[existingIndex] = updatedEntry;
        }

        if (sortByName)
        {
            aggregatedEntries.Sort((left, right) =>
            {
                string leftName = string.IsNullOrWhiteSpace(left.ItemData.Name) ? left.ItemData.Id : left.ItemData.Name;
                string rightName = string.IsNullOrWhiteSpace(right.ItemData.Name) ? right.ItemData.Id : right.ItemData.Name;
                return string.Compare(leftName, rightName, StringComparison.Ordinal);
            });
        }

        Transform parent = itemRoot != null ? itemRoot : transform;
        for (int i = 0; i < aggregatedEntries.Count; i++)
        {
            RemainingEntryData entryData = aggregatedEntries[i];
            RemainingItem itemView = Instantiate(itemPrefab, parent);
            itemView.Set(entryData.ItemData, entryData.Count);

            itemViews.Add(itemView);
            entriesById[entryData.ItemData.Id] = new RemainingEntry
            {
                ItemData = entryData.ItemData,
                Count = entryData.Count,
                View = itemView
            };
        }
    }

    public void ConsumeCollectedItem(ItemData itemData)
    {
        if (string.IsNullOrWhiteSpace(itemData.Id))
        {
            return;
        }

        RemainingEntry entry;
        if (!entriesById.TryGetValue(itemData.Id, out entry))
        {
            return;
        }

        entry.Count = Mathf.Max(0, entry.Count - 1);
        if (entry.Count == 0)
        {
            RemoveEntry(itemData.Id, entry.View);
            return;
        }

        if (entry.View != null)
        {
            entry.View.SetCount(entry.Count);
        }

        entriesById[itemData.Id] = entry;
    }

    public bool TryGetItemAnimationTarget(string itemId, out RectTransform targetRect, out Image targetImage)
    {
        targetRect = null;
        targetImage = null;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        RemainingEntry entry;
        if (!entriesById.TryGetValue(itemId, out entry) || entry.View == null)
        {
            return false;
        }

        return entry.View.TryGetAnimationTarget(out targetRect, out targetImage);
    }

    public void ClearAll()
    {
        for (int i = itemViews.Count - 1; i >= 0; i--)
        {
            RemainingItem itemView = itemViews[i];
            if (itemView == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(itemView.gameObject);
            }
            else
            {
                DestroyImmediate(itemView.gameObject);
            }
        }

        itemViews.Clear();
        entriesById.Clear();
    }

    private void RemoveEntry(string itemId, RemainingItem itemView)
    {
        entriesById.Remove(itemId);
        itemViews.Remove(itemView);

        if (itemView == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(itemView.gameObject);
        }
        else
        {
            DestroyImmediate(itemView.gameObject);
        }
    }

    private struct RemainingEntry
    {
        public ItemData ItemData;
        public int Count;
        public RemainingItem View;
    }

    private struct RemainingEntryData
    {
        public ItemData ItemData;
        public int Count;
    }
}
