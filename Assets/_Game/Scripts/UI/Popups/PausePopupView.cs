using UnityEngine;
using UnityEngine.UI;

public sealed class PausePopupView : PopupView
{
    [SerializeField] private Button resumeButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(HandleResumeClicked);
        }
    }

    protected override void OnDisable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(HandleResumeClicked);
        }

        base.OnDisable();
    }

    private void HandleResumeClicked()
    {
        if (App.Sessions != null)
        {
            App.Sessions.ResumeSession();
        }

        if (App.Popups != null)
        {
            App.Popups.Close(PopupType.Pause);
        }
    }
}
