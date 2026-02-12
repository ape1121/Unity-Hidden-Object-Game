using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RemainingItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    private string itemId;
    private int count;

    public string ItemId => itemId;
    public int Count => count;

    public bool TryGetAnimationTarget(out RectTransform targetRect, out Image targetImage)
    {
        targetImage = iconImage;
        targetRect = iconImage != null ? iconImage.rectTransform : null;
        return targetRect != null;
    }

    public void Set(ItemData itemData, int remainingCount)
    {
        itemId = itemData.Id;
        count = Mathf.Max(0, remainingCount);

        if (iconImage != null)
        {
            iconImage.sprite = itemData.Icon;
            iconImage.enabled = itemData.Icon != null;
        }

        UpdateCountText();
        UpdateName(itemData);
    }

    public void SetCount(int remainingCount)
    {
        count = Mathf.Max(0, remainingCount);
        UpdateCountText();
    }

    public void Clear()
    {
        itemId = string.Empty;
        count = 0;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }

        gameObject.name = "RemainingItem_Empty";
    }

    private void UpdateCountText()
    {
        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }

    private void UpdateName(ItemData itemData)
    {
        if (!string.IsNullOrWhiteSpace(itemData.Id))
        {
            gameObject.name = "RemainingItem_" + itemData.Id;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
        }

        if (countText == null)
        {
            Transform countTransform = transform.Find("RemainingBox/RemainingText");
            if (countTransform != null)
            {
                countText = countTransform.GetComponent<TMP_Text>();
            }
        }
    }
#endif
}
