using UnityEngine;
using UnityEngine.UI;

public class MainUI : SceneUserInterface
{
    [SerializeField] private Button playButton;
    [SerializeField] private bool enterOnEnable = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (playButton != null)
        {
            playButton.onClick.AddListener(HandlePlayButtonClicked);
        }

        if (enterOnEnable)
        {
            Enter(true);
        }
    }

    protected override void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayButtonClicked);
        }

        base.OnDisable();
    }

    private void HandlePlayButtonClicked()
    {
        if (App.Scenes == null)
        {
            return;
        }

        App.Scenes.LoadGame();
    }
}
