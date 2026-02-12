using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UICollectionAnimator : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform animationLayer;
    [SerializeField] private Image flyItemPrefab;
    [SerializeField] private Camera worldCamera;
    [Header("Fly Tuning")]
    [SerializeField] private float popDuration = 0.14f;
    [SerializeField] private Ease popEase = Ease.OutBack;
    [SerializeField] private Ease popMoveEase = Ease.OutSine;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float popScaleMultiplier = 1.35f;
    [SerializeField] private float popHorizontalMin = 20f;
    [SerializeField] private float popHorizontalMax = 56f;
    [SerializeField] private Vector2 popVerticalRange = new Vector2(70f, 120f);
    [SerializeField] private Vector2 popArcHeightRange = new Vector2(22f, 54f);
    [SerializeField] private float flightDuration = 0.36f;
    [SerializeField] private Ease flightEase = Ease.InOutSine;
    [SerializeField] private Ease sizeEase = Ease.InOutQuad;
    [SerializeField] private float curveHeight = 140f;
    [SerializeField] private float curveHeightDistanceFactor = 0.15f;
    [SerializeField] private Vector2 fallbackFlySize = new Vector2(150f, 150f);
    [SerializeField] private Vector2 minimumFlySize = new Vector2(130f, 130f);

    public Tween PlayCollectToBoard(Vector3 sourceWorldPosition, Sprite icon, RectTransform targetRect)
    {
        return PlayCollectToBoard(sourceWorldPosition, icon, targetRect, null, false, default, 0f);
    }

    public Tween PlayCollectToBoard(Vector3 sourceWorldPosition, Sprite icon, RectTransform targetRect, Image targetImage)
    {
        return PlayCollectToBoard(sourceWorldPosition, icon, targetRect, targetImage, false, default, 0f);
    }

    public Tween PlayCollectToBoard(
        Vector3 sourceWorldPosition,
        Sprite icon,
        RectTransform targetRect,
        Image targetImage,
        bool hasSourceBounds,
        Bounds sourceBounds,
        float sourceZRotationDegrees = 0f)
    {
        ResolveReferences();

        if (targetCanvas == null || targetRect == null || icon == null)
        {
            return null;
        }

        RectTransform canvasRect = targetCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return null;
        }

        RectTransform parentRect = animationLayer != null ? animationLayer : canvasRect;
        if (parentRect.rect.width <= 0.01f || parentRect.rect.height <= 0.01f)
        {
            Debug.LogWarning("UICollectionAnimator: animationLayer has zero size, falling back to canvas root.");
            parentRect = canvasRect;
        }

        Image flyImage = CreateFlyImage(parentRect);
        if (flyImage == null)
        {
            return null;
        }

        flyImage.sprite = icon;
        flyImage.enabled = true;
        flyImage.raycastTarget = false;
        flyImage.preserveAspect = targetImage != null ? targetImage.preserveAspect : true;

        RectTransform flyRect = flyImage.rectTransform;
        Vector2 sourceSize = hasSourceBounds ? BoundsToRectLocalSize(sourceBounds, parentRect) : Vector2.zero;
        ConfigureFlyRect(flyRect, sourceSize);
        Vector2 startSize = flyRect.rect.size;
        Vector2 targetSize = GetTargetRenderedSize(targetRect, targetImage, icon, parentRect);
        if (targetSize.x <= 1f || targetSize.y <= 1f)
        {
            targetSize = startSize;
        }

        Vector2 startLocal = WorldToRectLocalPoint(sourceWorldPosition, parentRect);
        Vector2 targetLocal = RectToRectLocalCenterPoint(targetRect, parentRect);
        Vector2 popOffset = GetRandomPopOffset();
        Vector2 popEndLocal = startLocal + popOffset;
        Vector2 popControlPoint = CalculatePopControlPoint(startLocal, popEndLocal, popOffset.x);
        Vector2 controlPoint = CalculateControlPoint(popEndLocal, targetLocal, popOffset.x);

        flyRect.anchoredPosition = startLocal;
        flyRect.localScale = Vector3.one * startScale;
        flyRect.localRotation = Quaternion.Euler(0f, 0f, sourceZRotationDegrees);
        flyRect.SetAsLastSibling();

        float popT = 0f;
        Tween popMoveTween = DOTween.To(() => popT, value =>
        {
            popT = value;
            flyRect.anchoredPosition = EvaluateQuadraticBezier(startLocal, popControlPoint, popEndLocal, popT);
        }, 1f, popDuration).SetEase(popMoveEase);

        float bezierT = 0f;
        Tween moveTween = DOTween.To(() => bezierT, value =>
        {
            bezierT = value;
            flyRect.anchoredPosition = EvaluateQuadraticBezier(popEndLocal, controlPoint, targetLocal, bezierT);
        }, 1f, flightDuration).SetEase(flightEase);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(flyRect.DOScale(startScale * popScaleMultiplier, popDuration).SetEase(popEase));
        sequence.Join(popMoveTween);
        sequence.Append(moveTween);
        sequence.Join(flyRect.DOScale(1f, flightDuration).SetEase(Ease.InQuad));
        sequence.Join(flyRect.DOSizeDelta(targetSize, flightDuration).SetEase(sizeEase));
        sequence.Join(flyRect.DOLocalRotate(Vector3.zero, flightDuration).SetEase(Ease.OutSine));
        sequence.OnComplete(() =>
        {
            if (flyImage != null)
            {
                Destroy(flyImage.gameObject);
            }
        });
        sequence.OnKill(() =>
        {
            if (flyImage != null)
            {
                Destroy(flyImage.gameObject);
            }
        });

        return sequence;
    }

    private Image CreateFlyImage(RectTransform parent)
    {
        Image flyImage;
        if (flyItemPrefab != null)
        {
            flyImage = Instantiate(flyItemPrefab, parent);
        }
        else
        {
            GameObject flyObject = new GameObject("FlyItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform flyRect = flyObject.GetComponent<RectTransform>();
            flyRect.SetParent(parent, false);
            flyRect.sizeDelta = new Vector2(96f, 96f);
            flyImage = flyObject.GetComponent<Image>();
            flyImage.preserveAspect = true;
            flyImage.raycastTarget = false;
        }

        return flyImage;
    }

    private void ConfigureFlyRect(RectTransform flyRect, Vector2 sourceSize)
    {
        if (flyRect == null)
        {
            return;
        }

        flyRect.anchorMin = new Vector2(0.5f, 0.5f);
        flyRect.anchorMax = new Vector2(0.5f, 0.5f);
        flyRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 currentSize = sourceSize;
        if (currentSize.x <= 1f || currentSize.y <= 1f)
        {
            currentSize = flyRect.rect.size;
        }

        if (currentSize.x <= 1f || currentSize.y <= 1f)
        {
            currentSize = fallbackFlySize;
        }

        currentSize = new Vector2(
            Mathf.Max(currentSize.x, minimumFlySize.x),
            Mathf.Max(currentSize.y, minimumFlySize.y));

        flyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize.x);
        flyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize.y);
    }

    private Vector2 BoundsToRectLocalSize(Bounds bounds, RectTransform targetRect)
    {
        Vector3 bottomLeftWorld = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
        Vector3 topRightWorld = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);

        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(worldCamera != null ? worldCamera : Camera.main, bottomLeftWorld);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(worldCamera != null ? worldCamera : Camera.main, topRightWorld);

        Vector2 bottomLeftLocal = ScreenToRectLocalPoint(bottomLeftScreen, targetRect);
        Vector2 topRightLocal = ScreenToRectLocalPoint(topRightScreen, targetRect);

        return new Vector2(
            Mathf.Abs(topRightLocal.x - bottomLeftLocal.x),
            Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
    }

    private Vector2 GetRandomPopOffset()
    {
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        float horizontalMagnitude = Random.Range(popHorizontalMin, popHorizontalMax);
        float horizontal = horizontalDirection * horizontalMagnitude;
        float vertical = Random.Range(popVerticalRange.x, popVerticalRange.y);
        return new Vector2(horizontal, vertical);
    }

    private Vector2 CalculatePopControlPoint(Vector2 start, Vector2 end, float popHorizontal)
    {
        float arcLift = Random.Range(popArcHeightRange.x, popArcHeightRange.y);
        Vector2 midpoint = (start + end) * 0.5f;
        Vector2 horizontalBias = Vector2.right * popHorizontal * 0.12f;
        return midpoint + (Vector2.up * arcLift) + horizontalBias;
    }

    private Vector2 CalculateControlPoint(Vector2 start, Vector2 end, float popHorizontal)
    {
        Vector2 midpoint = (start + end) * 0.5f;
        float distance = Vector2.Distance(start, end);
        float height = curveHeight + (distance * curveHeightDistanceFactor);
        return midpoint + (Vector2.up * height) + (Vector2.right * popHorizontal * 0.28f);
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;
        return (oneMinusT * oneMinusT * start) + (2f * oneMinusT * t * control) + (t * t * end);
    }

    private Vector2 WorldToRectLocalPoint(Vector3 worldPosition, RectTransform targetRect)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera != null ? worldCamera : Camera.main, worldPosition);
        return ScreenToRectLocalPoint(screenPoint, targetRect);
    }

    private Vector2 RectToRectLocalCenterPoint(RectTransform sourceRect, RectTransform targetRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        sourceRect.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(GetCanvasEventCamera(), worldCorners[i]);
            Vector2 local = ScreenToRectLocalPoint(screen, targetRect);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return (min + max) * 0.5f;
    }

    private Vector2 RectToRectLocalSize(RectTransform sourceRect, RectTransform targetRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        sourceRect.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(GetCanvasEventCamera(), worldCorners[i]);
            Vector2 local = ScreenToRectLocalPoint(screen, targetRect);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return max - min;
    }

    private Vector2 GetTargetRenderedSize(
        RectTransform targetRect,
        Image targetImage,
        Sprite icon,
        RectTransform parentRect)
    {
        Vector2 targetRectSize = RectToRectLocalSize(targetRect, parentRect);
        if (targetImage == null || !targetImage.preserveAspect)
        {
            return targetRectSize;
        }

        Sprite aspectSprite = icon != null ? icon : targetImage.sprite;
        if (aspectSprite == null || targetRectSize.x <= 1f || targetRectSize.y <= 1f)
        {
            return targetRectSize;
        }

        float spriteAspect = aspectSprite.rect.width / Mathf.Max(1f, aspectSprite.rect.height);
        if (spriteAspect <= 0f)
        {
            return targetRectSize;
        }

        float targetAspect = targetRectSize.x / targetRectSize.y;
        if (targetAspect > spriteAspect)
        {
            return new Vector2(targetRectSize.y * spriteAspect, targetRectSize.y);
        }

        return new Vector2(targetRectSize.x, targetRectSize.x / spriteAspect);
    }

    private Vector2 ScreenToRectLocalPoint(Vector2 screenPoint, RectTransform targetRect)
    {
        Camera eventCamera = GetCanvasEventCamera();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPoint, eventCamera, out localPoint);
        return localPoint;
    }

    private Camera GetCanvasEventCamera()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return targetCanvas.worldCamera;
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        popHorizontalMin = Mathf.Max(0f, popHorizontalMin);
        if (popHorizontalMax < popHorizontalMin)
        {
            popHorizontalMax = popHorizontalMin;
        }

        if (popVerticalRange.y < popVerticalRange.x)
        {
            popVerticalRange.y = popVerticalRange.x;
        }

        popArcHeightRange.x = Mathf.Max(0f, popArcHeightRange.x);
        if (popArcHeightRange.y < popArcHeightRange.x)
        {
            popArcHeightRange.y = popArcHeightRange.x;
        }
    }
#endif
}
