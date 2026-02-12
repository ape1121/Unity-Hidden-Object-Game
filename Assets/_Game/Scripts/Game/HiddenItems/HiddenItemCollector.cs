using System;
using UnityEngine;

public class HiddenItemCollector : MonoBehaviour
{
    [SerializeField] private HiddenItemInput hiddenItemInput;
    [SerializeField] private bool disableCollectedObject = true;

    public event Func<HiddenItem, bool> OnCanCollectHiddenItem;
    public event Action<HiddenItem> OnHiddenItemCollected;
    public event Action<ItemData> OnItemDataCollected;

    private void OnEnable()
    {
        if (hiddenItemInput != null)
        {
            hiddenItemInput.OnHiddenItemClicked += HandleHiddenItemClicked;
        }
    }

    private void OnDisable()
    {
        if (hiddenItemInput != null)
        {
            hiddenItemInput.OnHiddenItemClicked -= HandleHiddenItemClicked;
        }
    }

    private void HandleHiddenItemClicked(HiddenItem hiddenItem)
    {
        if (hiddenItem == null || !CanCollectHiddenItem(hiddenItem) || !hiddenItem.TryMarkCollected())
        {
            return;
        }

        OnHiddenItemCollected?.Invoke(hiddenItem);
        OnItemDataCollected?.Invoke(hiddenItem.ItemData);

        if (disableCollectedObject)
        {
            hiddenItem.gameObject.SetActive(false);
        }
        else
        {
            Destroy(hiddenItem.gameObject);
        }
    }

    private bool CanCollectHiddenItem(HiddenItem hiddenItem)
    {
        if (OnCanCollectHiddenItem == null)
        {
            return true;
        }

        Delegate[] listeners = OnCanCollectHiddenItem.GetInvocationList();
        for (int i = 0; i < listeners.Length; i++)
        {
            var listener = listeners[i] as Func<HiddenItem, bool>;
            if (listener == null)
            {
                continue;
            }

            if (!listener.Invoke(hiddenItem))
            {
                return false;
            }
        }

        return true;
    }
}
