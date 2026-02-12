using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private HiddenItemSpawner hiddenItemSpawner;
    [SerializeField] private HiddenItemCollector hiddenItemCollector;
    [SerializeField] private MatchBoard matchBoard;
    [SerializeField] private MatchBoardAnimator matchBoardAnimator;
    [SerializeField] private RemainingItems remainingItems;
    [SerializeField] private UICollectionAnimator collectionAnimator;
    [Header("Collection Timing")]
    [SerializeField, Min(0f)] private float boardHitLeadTime = 0.025f;

    private bool initialized;
    private bool processingCollectedQueue;
    private bool resolvingMatches;
    private bool blockBoardCommitsDuringResolve;
    private int activeCollectionAnimations;
    private Coroutine resolveMatchesCoroutine;
    private readonly Queue<PendingCollection> pendingCollections = new Queue<PendingCollection>();
    private readonly Queue<ItemData> deferredBoardCommits = new Queue<ItemData>();
    private readonly List<string> projectedBoardIds = new List<string>();
    private readonly List<MatchBoard.MatchInfo> matchWaveBuffer = new List<MatchBoard.MatchInfo>();
    private readonly List<int> committedInsertIndexBuffer = new List<int>();
    private int projectedBoardCapacity;
    private bool hasProjectedBoardState;
    private bool HasPendingCollections => pendingCollections.Count > 0;
    private bool HasDeferredBoardCommits => deferredBoardCommits.Count > 0;
    private bool HasActiveCollectionAnimations => activeCollectionAnimations > 0;
    private bool IsBoardCommitLocked => blockBoardCommitsDuringResolve;

    public void Configure(
        HiddenItemSpawner spawner,
        HiddenItemCollector collector,
        MatchBoard board,
        RemainingItems remaining)
    {
        hiddenItemSpawner = spawner;
        hiddenItemCollector = collector;
        matchBoard = board;
        remainingItems = remaining;
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        ResolveMissingReferences();
        ReportMissingReferences();
        SubscribeToEvents();

        HandleItemsSpawned(hiddenItemSpawner != null ? hiddenItemSpawner.SpawnedItems : null);
        initialized = true;
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
        deferredBoardCommits.Clear();
        processingCollectedQueue = false;
        resolvingMatches = false;
        blockBoardCommitsDuringResolve = false;
        activeCollectionAnimations = 0;
        resolveMatchesCoroutine = null;
        projectedBoardIds.Clear();
        matchWaveBuffer.Clear();
        committedInsertIndexBuffer.Clear();
        projectedBoardCapacity = 0;
        hasProjectedBoardState = false;
    }

    private void OnDisable()
    {
        Shutdown();
    }

    private void ResolveMissingReferences()
    {
        if (hiddenItemSpawner == null)
        {
            hiddenItemSpawner = FindFirstObjectByType<HiddenItemSpawner>();
        }

        if (hiddenItemCollector == null)
        {
            hiddenItemCollector = FindFirstObjectByType<HiddenItemCollector>();
        }

        if (matchBoard == null)
        {
            matchBoard = FindFirstObjectByType<MatchBoard>();
        }

        if (matchBoardAnimator == null)
        {
            matchBoardAnimator = FindFirstObjectByType<MatchBoardAnimator>();
        }

        if (remainingItems == null)
        {
            remainingItems = FindFirstObjectByType<RemainingItems>();
        }

        if (collectionAnimator == null)
        {
            collectionAnimator = FindFirstObjectByType<UICollectionAnimator>();
        }
    }

    private void ReportMissingReferences()
    {
        if (hiddenItemSpawner == null)
        {
            Debug.LogWarning("UIManager: HiddenItemSpawner reference is missing.");
        }

        if (hiddenItemCollector == null)
        {
            Debug.LogWarning("UIManager: HiddenItemCollector reference is missing.");
        }

        if (matchBoard == null)
        {
            Debug.LogWarning("UIManager: MatchBoard reference is missing.");
        }

        if (remainingItems == null)
        {
            Debug.LogWarning("UIManager: RemainingItems reference is missing.");
        }

        if (matchBoardAnimator == null)
        {
            Debug.LogWarning("UIManager: MatchBoardAnimator reference is missing. Match resolve animation will be skipped.");
        }

        if (collectionAnimator == null)
        {
            Debug.LogWarning("UIManager: UICollectionAnimator reference is missing. Collection will skip fly animation.");
        }
    }

    private void SubscribeToEvents()
    {
        if (hiddenItemSpawner != null)
        {
            hiddenItemSpawner.OnItemsSpawned += HandleItemsSpawned;
        }

        if (hiddenItemCollector != null)
        {
            hiddenItemCollector.OnCanCollectHiddenItem += CanCollectHiddenItem;
            hiddenItemCollector.OnHiddenItemCollected += HandleHiddenItemCollected;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (hiddenItemSpawner != null)
        {
            hiddenItemSpawner.OnItemsSpawned -= HandleItemsSpawned;
        }

        if (hiddenItemCollector != null)
        {
            hiddenItemCollector.OnCanCollectHiddenItem -= CanCollectHiddenItem;
            hiddenItemCollector.OnHiddenItemCollected -= HandleHiddenItemCollected;
        }
    }

    private bool CanCollectHiddenItem(HiddenItem hiddenItem)
    {
        if (hiddenItem == null ||
            matchBoard == null ||
            string.IsNullOrWhiteSpace(hiddenItem.ItemId))
        {
            return true;
        }

        EnsureProjectedBoardState();
        int capacity = projectedBoardCapacity > 0 ? projectedBoardCapacity : GetMatchBoardTileCapacity();
        if (capacity <= 0)
        {
            return true;
        }

        int predictedOccupiedAfterQueue = GetPredictedOccupiedCountForCollectValidation();
        return predictedOccupiedAfterQueue < capacity;
    }

    private int GetPredictedOccupiedCountForCollectValidation()
    {
        int projectedOccupiedCount = hasProjectedBoardState ? projectedBoardIds.Count : GetBoardOccupiedCount();
        int unreservedQueuedCollections = pendingCollections.Count;
        return projectedOccupiedCount + unreservedQueuedCollections;
    }

    private void HandleItemsSpawned(System.Collections.Generic.IReadOnlyList<HiddenItem> spawnedItems)
    {
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
        SpriteRenderer sourceRenderer = hiddenItem.SpriteRenderer;
        if (sourceRenderer == null)
        {
            sourceRenderer = hiddenItem.GetComponentInChildren<SpriteRenderer>();
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
            SourceBounds = sourceBounds
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
        TryCommitAtDispatchStart(itemData, target, dispatchState);
        Tween flyTween = TryStartCollectionFlyTween(entry, itemData, target, dispatchState);
        activeCollectionAnimations++;
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
        int insertIndex = ReserveProjectedInsertIndex(itemId, out bool shouldCommitImmediatelyForShift);
        return new CollectionTarget
        {
            InsertIndex = insertIndex,
            ShouldCommitImmediatelyForShift = shouldCommitImmediatelyForShift,
            TargetTile = matchBoard != null ? matchBoard.GetTileIconRect(insertIndex) : null,
            TargetImage = matchBoard != null ? matchBoard.GetTileIconImage(insertIndex) : null
        };
    }

    private void TryCommitAtDispatchStart(ItemData itemData, CollectionTarget target, CollectionDispatchState state)
    {
        if (ShouldQueueImmediateDeferredCommitForResolve())
        {
            CommitCollection(itemData);
            state.CommittedDuringFlight = true;
            return;
        }

        if (!target.ShouldCommitImmediatelyForShift || ShouldDeferBoardCommit())
        {
            return;
        }

        CommitCollection(itemData);
        state.CommittedDuringFlight = true;

        if (collectionAnimator == null || target.InsertIndex < 0 || matchBoard == null)
        {
            return;
        }

        MatchTile delayedRevealTile = matchBoard.GetTile(target.InsertIndex);
        if (delayedRevealTile == null)
        {
            return;
        }

        delayedRevealTile.PushTemporaryIconHide();
        state.DelayedRevealTile = delayedRevealTile;
        state.RevealTileAfterFly = true;
    }

    private Tween TryStartCollectionFlyTween(
        PendingCollection entry,
        ItemData itemData,
        CollectionTarget target,
        CollectionDispatchState state)
    {
        if (collectionAnimator == null || target.TargetTile == null)
        {
            return null;
        }

        Tween flyTween = collectionAnimator.PlayCollectToBoard(
            entry.WorldPosition,
            itemData.Icon,
            target.TargetTile,
            target.TargetImage,
            entry.HasSourceBounds,
            entry.SourceBounds);

        if (flyTween == null || state.CommittedDuringFlight)
        {
            return flyTween;
        }

        float tweenDuration = flyTween.Duration(false);
        float commitAtSeconds = Mathf.Max(0f, tweenDuration - boardHitLeadTime);
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

            if (state.RevealTileAfterFly && state.DelayedRevealTile != null)
            {
                state.DelayedRevealTile.PopTemporaryIconHide();
            }

            if (!state.CommittedDuringFlight)
            {
                CommitCollection(itemData);
                state.CommittedDuringFlight = true;
            }

            activeCollectionAnimations = Mathf.Max(0, activeCollectionAnimations - 1);
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
        if (ShouldDeferBoardCommit())
        {
            deferredBoardCommits.Enqueue(itemData);
        }
        else
        {
            CommitCollectionToBoard(itemData);
        }

        remainingItems?.ConsumeCollectedItem(itemData);
    }

    private bool ShouldDeferBoardCommit()
    {
        return IsBoardCommitLocked;
    }

    private bool ShouldQueueImmediateDeferredCommitForResolve()
    {
        return resolvingMatches && ShouldDeferBoardCommit();
    }

    private void CommitCollectionToBoard(ItemData itemData)
    {
        CommitCollectionToBoard(itemData, out _);
    }

    private bool CommitCollectionToBoard(ItemData itemData, out int insertIndex)
    {
        insertIndex = -1;
        if (matchBoard == null)
        {
            return false;
        }

        insertIndex = matchBoard.GetInsertIndexForItem(itemData.Id);
        bool addedToBoard = matchBoard.TryAddItem(itemData);
        if (!addedToBoard)
        {
            Debug.LogWarning("UIManager: MatchBoard rejected collected item insert.");
            InvalidateProjectedBoardState();
            insertIndex = -1;
            return false;
        }

        return true;
    }

    private bool TryFlushDeferredBoardCommits()
    {
        return TryFlushDeferredBoardCommits(null);
    }

    private bool TryFlushDeferredBoardCommits(List<int> committedInsertIndices)
    {
        if (!HasDeferredBoardCommits)
        {
            return false;
        }

        if (ShouldDeferBoardCommit())
        {
            return false;
        }

        bool committedAny = false;
        while (deferredBoardCommits.Count > 0)
        {
            ItemData itemData = deferredBoardCommits.Dequeue();
            int insertIndex;
            if (CommitCollectionToBoard(itemData, out insertIndex))
            {
                committedAny = true;
                if (committedInsertIndices != null && insertIndex >= 0)
                {
                    committedInsertIndices.Add(insertIndex);
                }
            }
        }

        if (committedAny)
        {
            InvalidateProjectedBoardState();
        }

        return committedAny;
    }

    private void TryAdvanceUiWorkflow()
    {
        TryFlushDeferredBoardCommits();

        if (TryStartCollectionProcessing())
        {
            return;
        }

        TryStartResolveMatches();
    }

    private bool TryStartCollectionProcessing()
    {
        if (!CanProcessCollections())
        {
            return false;
        }

        processingCollectedQueue = true;
        StartCoroutine(ProcessCollectedQueue());
        return true;
    }

    private bool CanProcessCollections()
    {
        return HasPendingCollections && !processingCollectedQueue;
    }

    private void TryStartResolveMatches()
    {
        if (!CanResolveMatches())
        {
            return;
        }

        matchWaveBuffer.Clear();
        if (!matchBoard.TryGetAllMatches(matchWaveBuffer))
        {
            return;
        }

        resolveMatchesCoroutine = StartCoroutine(ResolvePendingMatchesRoutine());
    }

    private bool CanResolveMatches()
    {
        return matchBoard != null &&
               !resolvingMatches &&
               !processingCollectedQueue &&
               !HasPendingCollections &&
               !HasActiveCollectionAnimations &&
               resolveMatchesCoroutine == null;
    }

    private IEnumerator ResolvePendingMatchesRoutine()
    {
        if (matchBoard == null)
        {
            resolveMatchesCoroutine = null;
            yield break;
        }

        resolvingMatches = true;
        blockBoardCommitsDuringResolve = false;

        while (true)
        {
            matchWaveBuffer.Clear();
            if (!matchBoard.TryGetAllMatches(matchWaveBuffer))
            {
                break;
            }

            blockBoardCommitsDuringResolve = true;
            yield return PlayMatchWaveMergeAnimations(matchWaveBuffer);

            blockBoardCommitsDuringResolve = false;
            committedInsertIndexBuffer.Clear();
            bool committedAfterMerge = TryFlushDeferredBoardCommits(committedInsertIndexBuffer);
            if (committedAfterMerge)
            {
                ShiftMatchWaveIndicesForCommittedInserts(matchWaveBuffer, committedInsertIndexBuffer);
                yield return WaitForAnimatorIdle();
            }

            blockBoardCommitsDuringResolve = true;
            if (!matchBoard.TryResolveMatches(matchWaveBuffer))
            {
                blockBoardCommitsDuringResolve = false;
                TryFlushDeferredBoardCommits();
                continue;
            }

            InvalidateProjectedBoardState();

            blockBoardCommitsDuringResolve = false;
            TryFlushDeferredBoardCommits();

            yield return WaitForAnimatorIdle();
        }

        blockBoardCommitsDuringResolve = false;
        resolvingMatches = false;
        resolveMatchesCoroutine = null;
        InvalidateProjectedBoardState();
        TryAdvanceUiWorkflow();
    }

    private static void ShiftMatchWaveIndicesForCommittedInserts(
        List<MatchBoard.MatchInfo> matchInfos,
        List<int> committedInsertIndices)
    {
        if (matchInfos == null || committedInsertIndices == null || committedInsertIndices.Count == 0)
        {
            return;
        }

        committedInsertIndices.Sort();
        for (int insertEvent = 0; insertEvent < committedInsertIndices.Count; insertEvent++)
        {
            int insertedIndex = committedInsertIndices[insertEvent];
            for (int match = 0; match < matchInfos.Count; match++)
            {
                int[] indices = matchInfos[match].Indices;
                if (indices == null)
                {
                    continue;
                }

                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] >= insertedIndex)
                    {
                        indices[i]++;
                    }
                }
            }
        }
    }

    private IEnumerator PlayMatchWaveMergeAnimations(IReadOnlyList<MatchBoard.MatchInfo> matchWave)
    {
        if (matchBoardAnimator == null || matchWave == null || matchWave.Count == 0)
        {
            yield break;
        }

        yield return WaitForAnimatorIdle();
        for (int i = 0; i < matchWave.Count; i++)
        {
            matchBoardAnimator.PlayMatchMerge(matchWave[i].Indices);
        }

        yield return WaitForAnimatorIdle();
    }

    private IEnumerator WaitForAnimatorIdle()
    {
        if (matchBoardAnimator != null)
        {
            yield return matchBoardAnimator.WaitForIdle();
        }
    }

    private int ReserveProjectedInsertIndex(string itemId, out bool willShift)
    {
        willShift = false;
        if (matchBoard == null || string.IsNullOrWhiteSpace(itemId))
        {
            return -1;
        }

        EnsureProjectedBoardState();
        if (!hasProjectedBoardState || projectedBoardCapacity <= 0)
        {
            return -1;
        }

        int occupiedCount = projectedBoardIds.Count;
        if (occupiedCount >= projectedBoardCapacity)
        {
            return -1;
        }

        int lastSameItemIndex = -1;
        for (int i = 0; i < occupiedCount; i++)
        {
            if (projectedBoardIds[i] == itemId)
            {
                lastSameItemIndex = i;
            }
        }

        int insertIndex = lastSameItemIndex >= 0 ? lastSameItemIndex + 1 : occupiedCount;
        willShift = insertIndex < occupiedCount;
        projectedBoardIds.Insert(insertIndex, itemId);
        return insertIndex;
    }

    private void EnsureProjectedBoardState()
    {
        if (matchBoard == null)
        {
            InvalidateProjectedBoardState();
            return;
        }

        if (!hasProjectedBoardState || !HasActiveCollectionAnimations)
        {
            RebuildProjectedBoardState();
        }
    }

    private void RebuildProjectedBoardState()
    {
        projectedBoardIds.Clear();
        projectedBoardCapacity = GetMatchBoardTileCapacity();
        if (projectedBoardCapacity <= 0)
        {
            hasProjectedBoardState = false;
            return;
        }

        for (int index = 0; index < projectedBoardCapacity; index++)
        {
            MatchTile tile = matchBoard.GetTile(index);
            if (tile == null || !tile.IsOccupied)
            {
                break;
            }

            projectedBoardIds.Add(tile.ItemId);
        }

        hasProjectedBoardState = true;
    }

    private void InvalidateProjectedBoardState()
    {
        projectedBoardIds.Clear();
        projectedBoardCapacity = 0;
        hasProjectedBoardState = false;
    }

    private int GetMatchBoardTileCapacity()
    {
        if (matchBoard == null)
        {
            return 0;
        }

        int capacity = 0;
        while (true)
        {
            if (matchBoard.GetTile(capacity) == null)
            {
                break;
            }

            capacity++;
        }

        return capacity;
    }

    private int GetBoardOccupiedCount()
    {
        if (matchBoard == null)
        {
            return 0;
        }

        int capacity = GetMatchBoardTileCapacity();
        int occupied = 0;
        for (int index = 0; index < capacity; index++)
        {
            MatchTile tile = matchBoard.GetTile(index);
            if (tile == null || !tile.IsOccupied)
            {
                break;
            }

            occupied++;
        }

        return occupied;
    }

    private struct PendingCollection
    {
        public Vector3 WorldPosition;
        public ItemData ItemData;
        public bool HasSourceBounds;
        public Bounds SourceBounds;
    }

    private struct CollectionTarget
    {
        public int InsertIndex;
        public bool ShouldCommitImmediatelyForShift;
        public RectTransform TargetTile;
        public Image TargetImage;
    }

    private sealed class CollectionDispatchState
    {
        public bool CommittedDuringFlight;
        public bool RevealTileAfterFly;
        public MatchTile DelayedRevealTile;
        public bool Finalized;
    }
}
