using System.Collections.Generic;
using UnityEngine;

public sealed class PopupManager
{
    private readonly Dictionary<PopupType, PopupView> popupByType = new Dictionary<PopupType, PopupView>();
    private readonly Canvas popupCanvas;

    public PopupManager(AppConfig config)
    {
        if (config == null || config.PopupCanvasPrefab == null)
        {
            return;
        }

        popupCanvas = Object.Instantiate(config.PopupCanvasPrefab);
        Object.DontDestroyOnLoad(popupCanvas.gameObject);

        BuildPopupRegistry(config.PopupPrefabs);
        CloseAll(true);
    }

    public void Shutdown()
    {
        popupByType.Clear();

        if (popupCanvas != null)
        {
            Object.Destroy(popupCanvas.gameObject);
        }
    }

    public bool Open(PopupType popupType, bool instant = false)
    {
        if (!TryGet(popupType, out PopupView popupView))
        {
            return false;
        }

        popupView.Open(instant);
        return true;
    }

    public bool Close(PopupType popupType, bool instant = false)
    {
        if (!TryGet(popupType, out PopupView popupView))
        {
            return false;
        }

        popupView.Close(instant);
        return true;
    }

    public void CloseAll(bool instant = false)
    {
        foreach (KeyValuePair<PopupType, PopupView> pair in popupByType)
        {
            PopupView popupView = pair.Value;
            if (popupView == null)
            {
                continue;
            }

            popupView.Close(instant);
        }
    }

    public bool TryGet(PopupType popupType, out PopupView popupView)
    {
        popupView = null;
        if (popupType == PopupType.None)
        {
            return false;
        }

        if (!popupByType.TryGetValue(popupType, out popupView))
        {
            return false;
        }

        return popupView != null;
    }

    private void BuildPopupRegistry(IReadOnlyList<PopupView> popupPrefabs)
    {
        if (popupPrefabs == null || popupCanvas == null)
        {
            return;
        }

        for (int i = 0; i < popupPrefabs.Count; i++)
        {
            PopupView popupPrefab = popupPrefabs[i];
            if (popupPrefab == null)
            {
                continue;
            }

            PopupView popupInstance = Object.Instantiate(popupPrefab, popupCanvas.transform, false);
            popupInstance.name = popupPrefab.name;
            Register(popupInstance);
        }
    }

    private void Register(PopupView popupView)
    {
        if (popupView == null)
        {
            return;
        }

        PopupType popupType = popupView.PopupType;
        if (popupType == PopupType.None)
        {
            return;
        }

        popupByType[popupType] = popupView;
    }
}
