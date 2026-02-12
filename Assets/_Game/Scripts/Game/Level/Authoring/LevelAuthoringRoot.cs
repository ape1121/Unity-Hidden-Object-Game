using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LevelAuthoringRoot : MonoBehaviour
{
    [SerializeField] private LevelData levelData;
    [SerializeField] private AllItems allItems;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Transform markerRoot;
    [SerializeField] private LevelItemMarker markerPrefab;

    public LevelData LevelData => levelData;

    public void ApplyBackgroundFromData()
    {
        if (levelData == null || backgroundRenderer == null)
        {
            return;
        }

        backgroundRenderer.sprite = levelData.BackgroundSprite;
        backgroundRenderer.transform.position = levelData.BackgroundPosition;
        backgroundRenderer.transform.localScale = levelData.BackgroundScale;
        backgroundRenderer.sortingOrder = levelData.BackgroundSortingOrder;
    }

    public void CaptureBackgroundToData()
    {
        if (levelData == null || backgroundRenderer == null)
        {
            return;
        }

        levelData.SetBackgroundState(
            backgroundRenderer.sprite,
            backgroundRenderer.transform.position,
            backgroundRenderer.transform.localScale,
            backgroundRenderer.sortingOrder);
    }

    public void LoadMarkersFromData()
    {
        if (levelData == null || markerRoot == null)
        {
            return;
        }

        ClearMarkers();
        IReadOnlyList<LevelItemPlacement> placements = levelData.ItemPlacements;
        for (int i = 0; i < placements.Count; i++)
        {
            LevelItemMarker marker = CreateMarker();
            marker.ApplyPlacement(placements[i]);
            marker.RefreshIcon(allItems);
        }
    }

    public void SaveMarkersToData()
    {
        if (levelData == null || markerRoot == null)
        {
            return;
        }

        LevelItemMarker[] markers = markerRoot.GetComponentsInChildren<LevelItemMarker>(true);
        var placements = new List<LevelItemPlacement>(markers.Length);
        for (int i = 0; i < markers.Length; i++)
        {
            placements.Add(markers[i].ToPlacement());
        }

        levelData.SetItemPlacements(placements.ToArray());
    }

    public void RefreshMarkerIcons()
    {
        if (markerRoot == null)
        {
            return;
        }

        LevelItemMarker[] markers = markerRoot.GetComponentsInChildren<LevelItemMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            markers[i].RefreshIcon(allItems);
        }
    }

    private LevelItemMarker CreateMarker()
    {
        LevelItemMarker marker;
        if (markerPrefab != null)
        {
            marker = Instantiate(markerPrefab, markerRoot);
        }
        else
        {
            var markerGameObject = new GameObject("ItemMarker");
            markerGameObject.transform.SetParent(markerRoot, false);
            marker = markerGameObject.AddComponent<LevelItemMarker>();
            markerGameObject.AddComponent<SpriteRenderer>();
        }

        return marker;
    }

    private void ClearMarkers()
    {
        if (markerRoot == null)
        {
            return;
        }

        for (int i = markerRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = markerRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
