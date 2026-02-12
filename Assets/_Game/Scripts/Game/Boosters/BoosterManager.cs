using System.Collections.Generic;

public sealed class BoosterManager
{
    private HiddenItemSpawner hiddenItemSpawner;
    private HiddenItemCollector hiddenItemCollector;

    public void Initialize(HiddenItemSpawner spawner, HiddenItemCollector collector)
    {
        hiddenItemSpawner = spawner;
        hiddenItemCollector = collector;
    }

    public bool UseBooster()
    {
        if (hiddenItemSpawner == null || hiddenItemCollector == null)
        {
            return false;
        }

        IReadOnlyList<HiddenItem> spawnedItems = hiddenItemSpawner.SpawnedItems;
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            HiddenItem hiddenItem = spawnedItems[i];
            if (hiddenItem == null || hiddenItem.IsCollected || !hiddenItem.gameObject.activeInHierarchy)
            {
                continue;
            }

            return hiddenItemCollector.TryCollect(hiddenItem);
        }

        return false;
    }
}
