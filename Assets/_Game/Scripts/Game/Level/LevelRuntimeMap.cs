using UnityEngine;

public class LevelRuntimeMap : MonoBehaviour
{
    [SerializeField] private LevelData levelData;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private HiddenItemSpawner hiddenItemSpawner;
    [SerializeField] private CameraRigController cameraRigController;

    public void SetLevel(LevelData newLevelData)
    {
        levelData = newLevelData;
    }

    public void InitializeLevel()
    {
        ApplyLevel();
    }

    public void ApplyLevel()
    {
        if (levelData == null)
        {
            return;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = levelData.BackgroundSprite;
            backgroundRenderer.transform.position = levelData.BackgroundPosition;
            backgroundRenderer.transform.localScale = levelData.BackgroundScale;
            backgroundRenderer.sortingOrder = levelData.BackgroundSortingOrder;
        }

        if (hiddenItemSpawner != null)
        {
            hiddenItemSpawner.SetLevel(levelData);
            hiddenItemSpawner.Spawn();
        }

        if (cameraRigController != null)
        {
            cameraRigController.SetBounds(levelData.CameraBoundsMin, levelData.CameraBoundsMax);
        }
    }
}
