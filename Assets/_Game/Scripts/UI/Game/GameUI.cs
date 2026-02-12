using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameUI : SceneUserInterface
{
    [Header("Dependencies")]
    [SerializeField] private UICollectionAnimator collectionAnimator;

    [Header("Collection Timing")]
    [SerializeField, Min(0f), FormerlySerializedAs("boardHitLeadTime")]
    private float targetHitLeadTime = 0.025f;

    [Header("Actions")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button boosterButton;
    [SerializeField] private RectTransform pauseButtonContainer;
    [SerializeField] private RectTransform goldContainer;
    [SerializeField] private RectTransform itemInventory;

    [Header("Panel Transitions")]
    [SerializeField] private bool enterOnEnable = true;
    [SerializeField] private bool enterInstantOnEnable = false;
    [SerializeField, Min(0f)] private float panelEnterDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelExitDuration = 0.22f;
    [SerializeField, Min(0f)] private float leftSlideDistance = 180f;
    [SerializeField, Min(0f)] private float rightSlideDistance = 180f;
    [SerializeField, Min(0f)] private float bottomSlideDistance = 180f;
    [SerializeField] private Ease panelEnterEase = Ease.OutCubic;
    [SerializeField] private Ease panelExitEase = Ease.InCubic;

    private bool initialized;
    private bool processingCollectedQueue;
    private readonly Queue<PendingCollection> pendingCollections = new Queue<PendingCollection>();
    private GameManager gameManager;
    private Action pauseRequestedHandler;
    private Action homeRequestedHandler;
    private Action boosterRequestedHandler;
    private Sequence panelTransitionSequence;
    private bool panelAnchorsCached;
    private Vector2 homeButtonAnchor;
    private Vector2 pauseButtonAnchor;
    private Vector2 goldContainerAnchor;
    private Vector2 itemInventoryAnchor;

    private bool HasPendingCollections => pendingCollections.Count > 0;

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        ResolveGameManagerReference();
        SubscribeToEvents();
        HiddenItemSpawner spawner = gameManager != null ? gameManager.HiddenItemSpawner : null;
        HandleItemsSpawned(spawner != null ? spawner.SpawnedItems : null);
        initialized = true;
    }

    public void BindGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public void BindGameplayActions(Action onPauseRequested, Action onHomeRequested, Action onBoosterRequested)
    {
        pauseRequestedHandler = onPauseRequested;
        homeRequestedHandler = onHomeRequested;
        boosterRequestedHandler = onBoosterRequested;
    }

    public void Shutdown()
    {
        if (!initialized)
        {
            return;
        }

        UnsubscribeFromEvents();
        StopAllCoroutines();
        ResetRuntimeState();
        initialized = false;
    }

    private void ResetRuntimeState()
    {
        pendingCollections.Clear();
        processingCollectedQueue = false;
    }

    public override void Enter(bool instant = false)
    {
        ResolveTransitionReferences();
        CachePanelAnchors();
        KillPanelTransitionTween();
        ApplyPanelHiddenState();

        if (instant || panelEnterDuration <= 0f)
        {
            ApplyPanelVisibleState();
            base.Enter(true);
            return;
        }

        base.Enter(false);
        panelTransitionSequence = DOTween.Sequence()
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnKill(() => panelTransitionSequence = null);

        JoinPanelMove(panelTransitionSequence, GetHomeButtonRect(), homeButtonAnchor, panelEnterDuration, panelEnterEase);
        JoinPanelMove(panelTransitionSequence, goldContainer, goldContainerAnchor, panelEnterDuration, panelEnterEase);
        JoinPanelMove(panelTransitionSequence, pauseButtonContainer, pauseButtonAnchor, panelEnterDuration, panelEnterEase);
        JoinPanelMove(panelTransitionSequence, itemInventory, itemInventoryAnchor, panelEnterDuration, panelEnterEase);
    }

    public override void Exit(bool instant = false)
    {
        ResolveTransitionReferences();
        CachePanelAnchors();
        KillPanelTransitionTween();

        if (instant || panelExitDuration <= 0f)
        {
            ApplyPanelHiddenState();
            base.Exit(true);
            return;
        }

        ApplyPanelVisibleState();
        base.Exit(false);
        panelTransitionSequence = DOTween.Sequence()
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnKill(() => panelTransitionSequence = null);

        JoinPanelMove(panelTransitionSequence, GetHomeButtonRect(), GetHomeHiddenAnchor(), panelExitDuration, panelExitEase);
        JoinPanelMove(panelTransitionSequence, goldContainer, GetGoldHiddenAnchor(), panelExitDuration, panelExitEase);
        JoinPanelMove(panelTransitionSequence, pauseButtonContainer, GetPauseHiddenAnchor(), panelExitDuration, panelExitEase);
        JoinPanelMove(panelTransitionSequence, itemInventory, GetInventoryHiddenAnchor(), panelExitDuration, panelExitEase);
    }

    public override float GetExitTransitionDuration(bool instant = false)
    {
        float baseDuration = base.GetExitTransitionDuration(instant);
        if (instant || panelExitDuration <= 0f)
        {
            return baseDuration;
        }

        return Mathf.Max(baseDuration, panelExitDuration);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(HandleHomeButtonClicked);
        }

        if (boosterButton != null)
        {
            boosterButton.onClick.AddListener(HandleBoosterButtonClicked);
        }

        if (enterOnEnable)
        {
            Enter(enterInstantOnEnable);
        }
    }

    protected override void OnDisable()
    {
        KillPanelTransitionTween();

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(HandleHomeButtonClicked);
        }

        if (boosterButton != null)
        {
            boosterButton.onClick.RemoveListener(HandleBoosterButtonClicked);
        }

        base.OnDisable();
        Shutdown();
    }

    protected override void OnDestroy()
    {
        KillPanelTransitionTween();
        base.OnDestroy();
    }

    private void HandleBoosterButtonClicked()
    {
        boosterRequestedHandler?.Invoke();
    }

    private void SubscribeToEvents()
    {
        HiddenItemSpawner spawner = gameManager != null ? gameManager.HiddenItemSpawner : null;
        if (spawner != null)
        {
            spawner.OnItemsSpawned += HandleItemsSpawned;
        }

        HiddenItemCollector collector = gameManager != null ? gameManager.HiddenItemCollector : null;
        if (collector != null)
        {
            collector.OnHiddenItemCollected += HandleHiddenItemCollected;
        }
    }

    private void UnsubscribeFromEvents()
    {
        HiddenItemSpawner spawner = gameManager != null ? gameManager.HiddenItemSpawner : null;
        if (spawner != null)
        {
            spawner.OnItemsSpawned -= HandleItemsSpawned;
        }

        HiddenItemCollector collector = gameManager != null ? gameManager.HiddenItemCollector : null;
        if (collector != null)
        {
            collector.OnHiddenItemCollected -= HandleHiddenItemCollected;
        }
    }

    private void HandleItemsSpawned(IReadOnlyList<HiddenItem> spawnedItems)
    {
        RemainingItems remainingItems = gameManager != null ? gameManager.RemainingItems : null;
        if (remainingItems != null)
        {
            remainingItems.BuildFromSpawnedItems(spawnedItems);
        }
    }

    private void HandleHiddenItemCollected(HiddenItem hiddenItem)
    {
        if (!TryBuildPendingCollection(hiddenItem, out PendingCollection pendingCollection))
        {
            return;
        }

        pendingCollections.Enqueue(pendingCollection);
        TryAdvanceUiWorkflow();
    }

    private static bool TryBuildPendingCollection(HiddenItem hiddenItem, out PendingCollection pendingCollection)
    {
        pendingCollection = default;
        if (hiddenItem == null)
        {
            return false;
        }

        bool hasSourceBounds = false;
        Bounds sourceBounds = default;
        Vector3 sourceWorldPosition = hiddenItem.transform.position;
        float sourceZRotationDegrees = hiddenItem.transform.eulerAngles.z;
        SpriteRenderer sourceRenderer = hiddenItem.SpriteRenderer;
        if (sourceRenderer == null)
        {
            sourceRenderer = hiddenItem.GetComponentInChildren<SpriteRenderer>();
        }

        if (sourceRenderer != null)
        {
            sourceZRotationDegrees = sourceRenderer.transform.eulerAngles.z;
        }

        if (sourceRenderer != null && sourceRenderer.sprite != null)
        {
            sourceBounds = sourceRenderer.bounds;
            hasSourceBounds = true;
            sourceWorldPosition = sourceBounds.center;
        }

        pendingCollection = new PendingCollection
        {
            WorldPosition = sourceWorldPosition,
            ItemData = hiddenItem.ItemData,
            HasSourceBounds = hasSourceBounds,
            SourceBounds = sourceBounds,
            SourceZRotationDegrees = sourceZRotationDegrees
        };
        return true;
    }

    private IEnumerator ProcessCollectedQueue()
    {
        while (HasPendingCollections)
        {
            PendingCollection entry = pendingCollections.Dequeue();
            DispatchCollection(entry);
        }

        processingCollectedQueue = false;
        TryAdvanceUiWorkflow();
        yield break;
    }

    private void DispatchCollection(PendingCollection entry)
    {
        ItemData itemData = entry.ItemData;
        CollectionTarget target = ResolveCollectionTarget(itemData.Id);
        CollectionDispatchState dispatchState = new CollectionDispatchState();
        Tween flyTween = TryStartCollectionFlyTween(entry, itemData, target, dispatchState);
        Action finalizeCollection = CreateFinalizeCollectionAction(itemData, dispatchState);

        if (flyTween != null)
        {
            StartCoroutine(FinalizeCollectionWhenTweenEnds(flyTween, finalizeCollection));
        }
        else
        {
            finalizeCollection();
        }
    }

    private CollectionTarget ResolveCollectionTarget(string itemId)
    {
        RectTransform targetRect = null;
        Image targetImage = null;

        RemainingItems remainingItems = gameManager != null ? gameManager.RemainingItems : null;
        if (remainingItems != null)
        {
            remainingItems.TryGetItemAnimationTarget(itemId, out targetRect, out targetImage);
        }

        return new CollectionTarget
        {
            TargetRect = targetRect,
            TargetImage = targetImage
        };
    }

    private Tween TryStartCollectionFlyTween(
        PendingCollection entry,
        ItemData itemData,
        CollectionTarget target,
        CollectionDispatchState state)
    {
        if (collectionAnimator == null || target.TargetRect == null)
        {
            return null;
        }

        Tween flyTween = collectionAnimator.PlayCollectToBoard(
            entry.WorldPosition,
            itemData.Icon,
            target.TargetRect,
            target.TargetImage,
            entry.HasSourceBounds,
            entry.SourceBounds,
            entry.SourceZRotationDegrees);

        if (flyTween == null)
        {
            return null;
        }

        float tweenDuration = flyTween.Duration(false);
        float commitAtSeconds = Mathf.Max(0f, tweenDuration - targetHitLeadTime);
        flyTween.OnUpdate(() =>
        {
            if (state.CommittedDuringFlight)
            {
                return;
            }

            if (flyTween.Elapsed(false) < commitAtSeconds)
            {
                return;
            }

            CommitCollection(itemData);
            state.CommittedDuringFlight = true;
        });

        return flyTween;
    }

    private Action CreateFinalizeCollectionAction(ItemData itemData, CollectionDispatchState state)
    {
        return () =>
        {
            if (state.Finalized)
            {
                return;
            }

            state.Finalized = true;

            if (!state.CommittedDuringFlight)
            {
                CommitCollection(itemData);
                state.CommittedDuringFlight = true;
            }

            TryAdvanceUiWorkflow();
        };
    }

    private static IEnumerator FinalizeCollectionWhenTweenEnds(Tween tween, Action finalizeAction)
    {
        if (tween != null)
        {
            yield return tween.WaitForCompletion();
        }

        finalizeAction?.Invoke();
    }

    private void CommitCollection(ItemData itemData)
    {
        RemainingItems remainingItems = gameManager != null ? gameManager.RemainingItems : null;
        remainingItems?.ConsumeCollectedItem(itemData);
    }

    private void HandleHomeButtonClicked()
    {
        homeRequestedHandler?.Invoke();
    }

    protected override void PauseGameAndOpenMenu()
    {
        pauseRequestedHandler?.Invoke();
    }

    private void TryAdvanceUiWorkflow()
    {
        if (processingCollectedQueue || !HasPendingCollections)
        {
            return;
        }

        processingCollectedQueue = true;
        StartCoroutine(ProcessCollectedQueue());
    }

    private void ResolveGameManagerReference()
    {
        if (gameManager == null)
        {
            gameManager = GetComponentInParent<GameManager>();
        }
    }

    private void ResolveTransitionReferences()
    {
        if (pauseButtonContainer == null && PauseButton != null)
        {
            pauseButtonContainer = PauseButton.transform as RectTransform;
        }

        if (goldContainer == null && GoldText != null)
        {
            RectTransform goldTextRect = GoldText.rectTransform;
            if (goldTextRect != null)
            {
                goldContainer = goldTextRect.parent as RectTransform;
                if (goldContainer == null)
                {
                    goldContainer = goldTextRect;
                }
            }
        }
    }

    private void CachePanelAnchors()
    {
        if (panelAnchorsCached)
        {
            return;
        }

        RectTransform homeRect = GetHomeButtonRect();
        if (homeRect != null)
        {
            homeButtonAnchor = homeRect.anchoredPosition;
        }

        if (pauseButtonContainer != null)
        {
            pauseButtonAnchor = pauseButtonContainer.anchoredPosition;
        }

        if (goldContainer != null)
        {
            goldContainerAnchor = goldContainer.anchoredPosition;
        }

        if (itemInventory != null)
        {
            itemInventoryAnchor = itemInventory.anchoredPosition;
        }

        panelAnchorsCached = true;
    }

    private void ApplyPanelVisibleState()
    {
        SetAnchoredPosition(GetHomeButtonRect(), homeButtonAnchor);
        SetAnchoredPosition(goldContainer, goldContainerAnchor);
        SetAnchoredPosition(pauseButtonContainer, pauseButtonAnchor);
        SetAnchoredPosition(itemInventory, itemInventoryAnchor);
    }

    private void ApplyPanelHiddenState()
    {
        SetAnchoredPosition(GetHomeButtonRect(), GetHomeHiddenAnchor());
        SetAnchoredPosition(goldContainer, GetGoldHiddenAnchor());
        SetAnchoredPosition(pauseButtonContainer, GetPauseHiddenAnchor());
        SetAnchoredPosition(itemInventory, GetInventoryHiddenAnchor());
    }

    private Vector2 GetHomeHiddenAnchor()
    {
        return homeButtonAnchor + (Vector2.left * leftSlideDistance);
    }

    private Vector2 GetGoldHiddenAnchor()
    {
        return goldContainerAnchor + (Vector2.left * leftSlideDistance);
    }

    private Vector2 GetPauseHiddenAnchor()
    {
        return pauseButtonAnchor + (Vector2.right * rightSlideDistance);
    }

    private Vector2 GetInventoryHiddenAnchor()
    {
        return itemInventoryAnchor + (Vector2.down * bottomSlideDistance);
    }

    private static void SetAnchoredPosition(RectTransform targetRect, Vector2 anchoredPosition)
    {
        if (targetRect == null)
        {
            return;
        }

        targetRect.anchoredPosition = anchoredPosition;
    }

    private static void JoinPanelMove(Sequence sequence, RectTransform targetRect, Vector2 endPosition, float duration, Ease ease)
    {
        if (sequence == null || targetRect == null)
        {
            return;
        }

        sequence.Join(targetRect.DOAnchorPos(endPosition, duration).SetEase(ease));
    }

    private void KillPanelTransitionTween()
    {
        if (panelTransitionSequence == null)
        {
            return;
        }

        panelTransitionSequence.Kill();
        panelTransitionSequence = null;
    }

    private RectTransform GetHomeButtonRect()
    {
        return homeButton != null ? homeButton.transform as RectTransform : null;
    }

    private struct PendingCollection
    {
        public Vector3 WorldPosition;
        public ItemData ItemData;
        public bool HasSourceBounds;
        public Bounds SourceBounds;
        public float SourceZRotationDegrees;
    }

    private struct CollectionTarget
    {
        public RectTransform TargetRect;
        public Image TargetImage;
    }

    private sealed class CollectionDispatchState
    {
        public bool CommittedDuringFlight;
        public bool Finalized;
    }
}
