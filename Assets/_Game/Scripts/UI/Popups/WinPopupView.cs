using UnityEngine;
using UnityEngine.UI;

public sealed class WinPopupView : PopupView
{
    [SerializeField] private Button mainMenuButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }
    }

    protected override void OnDisable()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        base.OnDisable();
    }

    private void HandleMainMenuClicked()
    {
        if (App.Sessions != null)
        {
            App.Sessions.ResetToIdle();
        }

        if (App.Popups != null)
        {
            App.Popups.Close(PopupType.Win);
        }

        if (App.Scenes != null)
        {
            App.Scenes.LoadMain();
        }
    }
}
