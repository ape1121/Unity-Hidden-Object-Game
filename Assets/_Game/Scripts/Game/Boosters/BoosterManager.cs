using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class BoosterManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Image boosterBackground;
    [SerializeField] private Image boosterSpotlightCircle;

    [Header("Booster Hint")]
    [SerializeField, Range(0f, 1f)] private float boosterBackgroundAlpha = 0.7f;
    [SerializeField, Min(0f)] private float boosterBackgroundFadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float boosterSpotlightFadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float boosterSpotlightHoldDuration = 1.1f;
    [SerializeField, Min(0f)] private float boosterSpotlightPadding = 80f;
    [SerializeField, Min(1f)] private float boosterSpotlightMinimumDiameter = 300f;
    [SerializeField] private Ease boosterHintEase = Ease.OutCubic;

    [Header("Booster Camera Focus")]
    [SerializeField] private bool useHighlightZoom = true;
    [SerializeField, Min(0f)] private float highlightPanDuration = 0.35f;
    [SerializeField, Min(0f)] private float highlightZoomDuration = 0.35f;
    [SerializeField, Min(0f)] private float highlightZoomSize = 7f;

    private Sequence boosterHintSequence;
    private GameManager gameManager;
    private Texture2D runtimeSpotlightTexture;
    private Sprite runtimeSpotlightSprite;
    private Texture2D runtimeBoosterMaskTexture;
    private Sprite runtimeBoosterMaskSprite;
    private Color[] runtimeBoosterMaskPixels;
    private float runtimeBoosterMaskCutoutNormalizedRadius = -1f;
    private HiddenItem boosterHintTarget;

    private const float DefaultBoosterMaskCutoutNormalizedRadius = 0.2f;
    private const float BoosterMaskCutoutRefreshThreshold = 0.0025f;
    private const float SpotlightRingInnerNormalizedRadius = 0.62f;
    private const int BoosterMaskTextureSize = 512;

    public void Configure(GameManager manager)
    {
        if (manager != null)
        {
            gameManager = manager;
        }
    }

    public bool TryUseBooster()
    {
        if (!TryGetTargetItem(out HiddenItem targetItem))
        {
            return false;
        }

        CameraRigController cameraController = ResolveCameraRigController();
        if (cameraController != null)
        {
            cameraController.FocusOnWorldPosition(
                targetItem.transform.position,
                useHighlightZoom ? highlightZoomSize : (float?)null,
                highlightPanDuration,
                highlightZoomDuration);
        }

        ShowBoosterHint(targetItem);
        return true;
    }

    public bool TryGetTargetItem(out HiddenItem targetItem)
    {
        targetItem = null;
        HiddenItemSpawner spawner = ResolveHiddenItemSpawner();
        if (spawner == null)
        {
            return false;
        }

        var spawnedItems = spawner.SpawnedItems;
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            HiddenItem hiddenItem = spawnedItems[i];
            if (hiddenItem == null || hiddenItem.IsCollected || !hiddenItem.gameObject.activeInHierarchy)
            {
                continue;
            }

            targetItem = hiddenItem;
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        StopBoosterHintSequence();
        HideBoosterHintVisuals();
    }

    private void OnDestroy()
    {
        StopBoosterHintSequence();
        ReleaseRuntimeAssets();
    }

    private void ShowBoosterHint(HiddenItem targetItem)
    {
        if (targetItem == null)
        {
            return;
        }

        EnsureBoosterHintVisuals();
        RectTransform root = ResolveOverlayRoot();
        if (boosterBackground == null || boosterSpotlightCircle == null || root == null)
        {
            return;
        }

        boosterHintTarget = targetItem;
        PositionBoosterSpotlight(targetItem);
        PlayBoosterHintSequence();
    }

    private void EnsureBoosterHintVisuals()
    {
        RectTransform root = ResolveOverlayRoot();
        if (root == null)
        {
            return;
        }

        if (boosterBackground == null)
        {
            boosterBackground = CreateRuntimeBoosterBackground(root);
        }

        if (boosterSpotlightCircle == null)
        {
            boosterSpotlightCircle = CreateRuntimeBoosterSpotlightCircle(root);
        }

        if (boosterBackground == null || boosterSpotlightCircle == null)
        {
            return;
        }

        ConfigureOverlayElementRect(boosterBackground.rectTransform);
        ConfigureOverlayElementRect(boosterSpotlightCircle.rectTransform);
        boosterBackground.sprite = GetOrCreateRuntimeBoosterMaskSprite(DefaultBoosterMaskCutoutNormalizedRadius);
        boosterBackground.type = Image.Type.Simple;
        boosterBackground.preserveAspect = false;
        boosterBackground.raycastTarget = false;
        boosterSpotlightCircle.raycastTarget = false;

        if (boosterSpotlightCircle.sprite == null)
        {
            boosterSpotlightCircle.sprite = GetOrCreateRuntimeSpotlightRingSprite();
        }
    }

    private RectTransform ResolveOverlayRoot()
    {
        GameManager manager = ResolveGameManager();
        if (manager != null && manager.UiOverlayRoot != null)
        {
            return manager.UiOverlayRoot;
        }

        if (boosterBackground != null)
        {
            RectTransform rootFromBackground = boosterBackground.transform.parent as RectTransform;
            if (rootFromBackground != null)
            {
                return rootFromBackground;
            }
        }

        return null;
    }

    private Image CreateRuntimeBoosterBackground(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        GameObject backgroundObject = new GameObject("BoosterBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(root, false);

        RectTransform rectTransform = backgroundObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.one * 100f;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        backgroundImage.raycastTarget = false;
        backgroundObject.transform.SetAsLastSibling();
        return backgroundImage;
    }

    private Image CreateRuntimeBoosterSpotlightCircle(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        GameObject circleObject = new GameObject("BoosterSpotlightCircle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        circleObject.transform.SetParent(root, false);

        RectTransform rectTransform = circleObject.GetComponent<RectTransform>();
        ConfigureOverlayElementRect(rectTransform);
        rectTransform.sizeDelta = Vector2.one * boosterSpotlightMinimumDiameter;

        Image circleImage = circleObject.GetComponent<Image>();
        circleImage.color = new Color(1f, 1f, 1f, 0f);
        circleImage.raycastTarget = false;
        return circleImage;
    }

    private void PositionBoosterSpotlight(HiddenItem targetItem)
    {
        RectTransform overlayRoot = ResolveOverlayRoot();
        if (boosterBackground == null || boosterSpotlightCircle == null || targetItem == null || overlayRoot == null)
        {
            return;
        }

        Camera worldCamera = ResolveWorldCamera();
        SpriteRenderer targetRenderer = targetItem.SpriteRenderer;
        Vector3 worldPosition = targetRenderer != null ? targetRenderer.bounds.center : targetItem.transform.position;
        Vector3 screenPosition = worldCamera != null
            ? worldCamera.WorldToScreenPoint(worldPosition)
            : RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        RectTransform backgroundRect = boosterBackground.rectTransform;
        RectTransform spotlightRect = boosterSpotlightCircle.rectTransform;
        Camera uiCamera = ResolveUiCamera();
        Vector2 localPoint = Vector2.zero;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRoot,
                screenPosition,
                uiCamera,
                out localPoint))
        {
            backgroundRect.anchoredPosition = localPoint;
            spotlightRect.anchoredPosition = localPoint;
        }

        float spotlightDiameter = Mathf.Max(1f, boosterSpotlightMinimumDiameter);
        if (targetRenderer != null && worldCamera != null)
        {
            Bounds bounds = targetRenderer.bounds;
            Vector3 minScreen = worldCamera.WorldToScreenPoint(bounds.min);
            Vector3 maxScreen = worldCamera.WorldToScreenPoint(bounds.max);

            float width = Mathf.Abs(maxScreen.x - minScreen.x);
            float height = Mathf.Abs(maxScreen.y - minScreen.y);
            float diameterFromBounds = Mathf.Max(width, height) + boosterSpotlightPadding;
            spotlightDiameter = Mathf.Max(spotlightDiameter, diameterFromBounds);
        }

        spotlightRect.sizeDelta = new Vector2(spotlightDiameter, spotlightDiameter);
        float spotlightInnerRadius = spotlightDiameter * 0.5f * SpotlightRingInnerNormalizedRadius;
        float maskHalfSize = UpdateBoosterMaskSize(backgroundRect, overlayRoot.rect, localPoint);
        float cutoutNormalizedRadius = Mathf.Clamp01(spotlightInnerRadius / Mathf.Max(1f, maskHalfSize));
        boosterBackground.sprite = GetOrCreateRuntimeBoosterMaskSprite(cutoutNormalizedRadius);
    }

    private static void ConfigureOverlayElementRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static float UpdateBoosterMaskSize(
        RectTransform backgroundRect,
        Rect overlayRect,
        Vector2 localPoint)
    {
        if (backgroundRect == null)
        {
            return 1f;
        }

        float halfWidth = overlayRect.width * 0.5f;
        float halfHeight = overlayRect.height * 0.5f;

        float requiredHalfByCoverage = Mathf.Max(
            halfWidth + Mathf.Abs(localPoint.x),
            halfHeight + Mathf.Abs(localPoint.y));

        float requiredHalfSize = Mathf.Max(1f, requiredHalfByCoverage);
        float requiredSize = Mathf.Max(1f, requiredHalfSize * 2f);

        backgroundRect.sizeDelta = new Vector2(requiredSize, requiredSize);
        return requiredHalfSize;
    }

    private void PlayBoosterHintSequence()
    {
        StopBoosterHintSequence();
        SetImageAlpha(boosterBackground, 0f);
        SetImageAlpha(boosterSpotlightCircle, 0f);
        boosterBackground.transform.SetAsLastSibling();
        boosterSpotlightCircle.transform.SetAsLastSibling();
        boosterBackground.gameObject.SetActive(true);
        boosterSpotlightCircle.gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(boosterBackground.DOFade(boosterBackgroundAlpha, boosterBackgroundFadeDuration).SetEase(boosterHintEase));
        sequence.Join(boosterSpotlightCircle.DOFade(1f, boosterSpotlightFadeDuration).SetEase(boosterHintEase));
        sequence.AppendInterval(boosterSpotlightHoldDuration);
        sequence.Append(boosterSpotlightCircle.DOFade(0f, boosterSpotlightFadeDuration * 0.8f).SetEase(Ease.InCubic));
        sequence.Join(boosterBackground.DOFade(0f, boosterBackgroundFadeDuration * 0.8f).SetEase(Ease.InCubic));
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.OnUpdate(UpdateBoosterHintTargetPosition);
        sequence.OnComplete(HideBoosterHintVisuals);
        sequence.OnKill(HideBoosterHintVisuals);
        boosterHintSequence = sequence;
    }

    private void StopBoosterHintSequence()
    {
        if (boosterHintSequence == null)
        {
            return;
        }

        boosterHintSequence.Kill();
        boosterHintSequence = null;
    }

    private void HideBoosterHintVisuals()
    {
        boosterHintTarget = null;

        if (boosterBackground != null)
        {
            SetImageAlpha(boosterBackground, 0f);
            boosterBackground.gameObject.SetActive(false);
        }

        if (boosterSpotlightCircle != null)
        {
            SetImageAlpha(boosterSpotlightCircle, 0f);
            boosterSpotlightCircle.gameObject.SetActive(false);
        }
    }

    private Camera ResolveWorldCamera()
    {
        CameraRigController cameraController = ResolveCameraRigController();
        if (cameraController != null && cameraController.TargetCamera != null)
        {
            return cameraController.TargetCamera;
        }

        return Camera.main;
    }

    private GameManager ResolveGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }

        return gameManager;
    }

    private HiddenItemSpawner ResolveHiddenItemSpawner()
    {
        GameManager manager = ResolveGameManager();
        return manager != null ? manager.HiddenItemSpawner : null;
    }

    private CameraRigController ResolveCameraRigController()
    {
        GameManager manager = ResolveGameManager();
        return manager != null ? manager.CameraRigController : null;
    }

    private Camera ResolveUiCamera()
    {
        RectTransform overlayRoot = ResolveOverlayRoot();
        if (overlayRoot == null)
        {
            return null;
        }

        Canvas canvas = overlayRoot.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void UpdateBoosterHintTargetPosition()
    {
        if (boosterHintTarget == null)
        {
            return;
        }

        if (!boosterHintTarget.gameObject.activeInHierarchy)
        {
            StopBoosterHintSequence();
            return;
        }

        PositionBoosterSpotlight(boosterHintTarget);
    }

    private Sprite GetOrCreateRuntimeSpotlightRingSprite()
    {
        if (runtimeSpotlightSprite != null)
        {
            return runtimeSpotlightSprite;
        }

        const int textureSize = 256;
        runtimeSpotlightTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        runtimeSpotlightTexture.name = "BoosterSpotlightTexture";
        runtimeSpotlightTexture.filterMode = FilterMode.Bilinear;
        runtimeSpotlightTexture.wrapMode = TextureWrapMode.Clamp;

        float halfSize = textureSize * 0.5f;
        float ringOuter = halfSize - 1f;
        float ringInner = ringOuter * SpotlightRingInnerNormalizedRadius;
        float featherRange = ringOuter - ringInner;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x + 0.5f) - halfSize;
                float dy = (y + 0.5f) - halfSize;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 0f;
                if (distance >= ringInner && distance <= ringOuter)
                {
                    float t = (distance - ringInner) / Mathf.Max(0.001f, featherRange);
                    alpha = Mathf.SmoothStep(0.9f, 0f, t);
                }

                pixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeSpotlightTexture.SetPixels(pixels);
        runtimeSpotlightTexture.Apply(false, false);

        runtimeSpotlightSprite = Sprite.Create(
            runtimeSpotlightTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);

        runtimeSpotlightSprite.name = "BoosterSpotlightSprite";
        return runtimeSpotlightSprite;
    }

    private Sprite GetOrCreateRuntimeBoosterMaskSprite(float cutoutNormalizedRadius)
    {
        float clampedCutoutNormalizedRadius = Mathf.Clamp(cutoutNormalizedRadius, 0.001f, 0.999f);

        if (runtimeBoosterMaskTexture == null)
        {
            runtimeBoosterMaskTexture = new Texture2D(BoosterMaskTextureSize, BoosterMaskTextureSize, TextureFormat.RGBA32, false);
            runtimeBoosterMaskTexture.name = "BoosterMaskTexture";
            runtimeBoosterMaskTexture.filterMode = FilterMode.Point;
            runtimeBoosterMaskTexture.wrapMode = TextureWrapMode.Clamp;
        }

        if (runtimeBoosterMaskSprite == null)
        {
            runtimeBoosterMaskSprite = Sprite.Create(
                runtimeBoosterMaskTexture,
                new Rect(0f, 0f, BoosterMaskTextureSize, BoosterMaskTextureSize),
                new Vector2(0.5f, 0.5f),
                BoosterMaskTextureSize);
            runtimeBoosterMaskSprite.name = "BoosterMaskSprite";
        }

        if (Mathf.Abs(runtimeBoosterMaskCutoutNormalizedRadius - clampedCutoutNormalizedRadius) > BoosterMaskCutoutRefreshThreshold)
        {
            RedrawBoosterMaskTexture(clampedCutoutNormalizedRadius);
            runtimeBoosterMaskCutoutNormalizedRadius = clampedCutoutNormalizedRadius;
        }

        return runtimeBoosterMaskSprite;
    }

    private void RedrawBoosterMaskTexture(float cutoutNormalizedRadius)
    {
        if (runtimeBoosterMaskTexture == null)
        {
            return;
        }

        int textureSize = runtimeBoosterMaskTexture.width;
        int pixelCount = textureSize * textureSize;
        if (runtimeBoosterMaskPixels == null || runtimeBoosterMaskPixels.Length != pixelCount)
        {
            runtimeBoosterMaskPixels = new Color[pixelCount];
        }

        float halfSize = textureSize * 0.5f;
        float cutoutRadius = halfSize * cutoutNormalizedRadius;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x + 0.5f) - halfSize;
                float dy = (y + 0.5f) - halfSize;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = distance <= cutoutRadius ? 0f : 1f;
                runtimeBoosterMaskPixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeBoosterMaskTexture.SetPixels(runtimeBoosterMaskPixels);
        runtimeBoosterMaskTexture.Apply(false, false);
    }

    private void ReleaseRuntimeAssets()
    {
        if (runtimeSpotlightSprite != null)
        {
            Destroy(runtimeSpotlightSprite);
            runtimeSpotlightSprite = null;
        }

        if (runtimeSpotlightTexture != null)
        {
            Destroy(runtimeSpotlightTexture);
            runtimeSpotlightTexture = null;
        }

        if (runtimeBoosterMaskSprite != null)
        {
            Destroy(runtimeBoosterMaskSprite);
            runtimeBoosterMaskSprite = null;
        }

        if (runtimeBoosterMaskTexture != null)
        {
            Destroy(runtimeBoosterMaskTexture);
            runtimeBoosterMaskTexture = null;
        }

        runtimeBoosterMaskPixels = null;
        runtimeBoosterMaskCutoutNormalizedRadius = -1f;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
