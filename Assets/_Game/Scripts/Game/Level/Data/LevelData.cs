using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "SearchIt/Level Data", order = 2)]
public class LevelData : ScriptableObject
{
    [SerializeField] private string levelId = "level_01";
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Vector3 backgroundPosition = Vector3.zero;
    [SerializeField] private Vector3 backgroundScale = Vector3.one;
    [SerializeField] private int backgroundSortingOrder = -100;
    [SerializeField] private Vector2 cameraBoundsMin = new Vector2(-20f, -12f);
    [SerializeField] private Vector2 cameraBoundsMax = new Vector2(20f, 12f);
    [SerializeField] private LevelItemPlacement[] itemPlacements = Array.Empty<LevelItemPlacement>();

    public string LevelId => levelId;
    public Sprite BackgroundSprite => backgroundSprite;
    public Vector3 BackgroundPosition => backgroundPosition;
    public Vector3 BackgroundScale => backgroundScale;
    public int BackgroundSortingOrder => backgroundSortingOrder;
    public Vector2 CameraBoundsMin => cameraBoundsMin;
    public Vector2 CameraBoundsMax => cameraBoundsMax;
    public IReadOnlyList<LevelItemPlacement> ItemPlacements => itemPlacements ?? Array.Empty<LevelItemPlacement>();

    public void SetBackgroundState(Sprite sprite, Vector3 position, Vector3 scale, int sortingOrder)
    {
        backgroundSprite = sprite;
        backgroundPosition = position;
        backgroundScale = scale;
        backgroundSortingOrder = sortingOrder;
    }

    public void SetItemPlacements(LevelItemPlacement[] placements)
    {
        itemPlacements = placements ?? Array.Empty<LevelItemPlacement>();
    }
}

[Serializable]
public struct LevelItemPlacement
{
    public string ItemId;
    public Vector3 Position;
    public Vector3 Scale;
    public float RotationZ;
    public int SortingOrder;
}
