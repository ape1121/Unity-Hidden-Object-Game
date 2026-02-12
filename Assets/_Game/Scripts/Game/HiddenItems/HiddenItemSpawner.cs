using System;
using System.Collections.Generic;
using UnityEngine;

public class HiddenItemSpawner : MonoBehaviour
{
    [SerializeField] private AllItems allItems;
    [SerializeField] private LevelData levelData;
    [SerializeField] private HiddenItem hiddenItemPrefab;
    [SerializeField] private Transform itemRoot;

    private readonly List<HiddenItem> spawnedItems = new List<HiddenItem>();

    public event Action<IReadOnlyList<HiddenItem>> OnItemsSpawned;

    public IReadOnlyList<HiddenItem> SpawnedItems => spawnedItems;

    public void SetLevel(LevelData newLevelData)
    {
        levelData = newLevelData;
    }

    public void Spawn()
    {
        Clear();

        if (allItems == null || levelData == null || hiddenItemPrefab == null)
        {
            OnItemsSpawned?.Invoke(spawnedItems);
            return;
        }

        Transform parent = itemRoot != null ? itemRoot : transform;
        IReadOnlyList<LevelItemPlacement> placements = levelData.ItemPlacements;

        for (int i = 0; i < placements.Count; i++)
        {
            LevelItemPlacement placement = placements[i];
            ItemData itemData;
            if (!allItems.TryGetById(placement.ItemId, out itemData))
            {
                continue;
            }

            HiddenItem spawnedItem = Instantiate(hiddenItemPrefab, parent);
            Transform spawnedTransform = spawnedItem.transform;
            spawnedTransform.position = placement.Position;
            spawnedTransform.rotation = Quaternion.Euler(0f, 0f, placement.RotationZ);
            spawnedTransform.localScale = placement.Scale == Vector3.zero ? Vector3.one : placement.Scale;

            SpriteRenderer spriteRenderer = spawnedItem.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = placement.SortingOrder;
            }

            spawnedItem.Initialize(itemData);
            spawnedItems.Add(spawnedItem);
        }

        OnItemsSpawned?.Invoke(spawnedItems);
    }

    public void Clear()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            HiddenItem item = spawnedItems[i];
            if (item == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(item.gameObject);
            }
            else
            {
                DestroyImmediate(item.gameObject);
            }
        }

        spawnedItems.Clear();
    }
}
