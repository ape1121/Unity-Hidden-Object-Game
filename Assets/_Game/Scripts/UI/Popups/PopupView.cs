using UnityEngine;

public class PopupView : CanvasGroupUserInterface
{
    [SerializeField] private PopupType popupType = PopupType.None;

    public PopupType PopupType => popupType;

    public void Open(bool instant = false)
    {
        Enter(instant);
    }

    public void Close(bool instant = false)
    {
        Exit(instant);
    }
}
