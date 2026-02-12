using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class SceneUserInterface : CanvasGroupUserInterface
{
    [Header("Actions")]
    [SerializeField] private Button pauseButton;

    [Header("Economy")]
    [SerializeField] private TMP_Text goldText;

    protected Button PauseButton => pauseButton;
    protected TMP_Text GoldText => goldText;

    protected override void OnEnable()
    {
        base.OnEnable();
        BindPauseButton();
        BindCoinEvents();
        RefreshGoldText();
    }

    protected override void OnDisable()
    {
        UnbindPauseButton();
        UnbindCoinEvents();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        UnbindPauseButton();
        UnbindCoinEvents();
        base.OnDestroy();
    }

    private void BindPauseButton()
    {
        if (pauseButton == null)
        {
            return;
        }

        pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
        pauseButton.onClick.AddListener(HandlePauseButtonClicked);
    }

    private void UnbindPauseButton()
    {
        if (pauseButton == null)
        {
            return;
        }

        pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
    }

    private void HandlePauseButtonClicked()
    {
        PauseGameAndOpenMenu();
    }

    protected virtual void PauseGameAndOpenMenu()
    {
        if (App.Sessions != null)
        {
            if (App.Sessions.State == GameSessionState.Completed)
            {
                return;
            }

            App.Sessions.PauseSession();
        }

        if (App.Popups != null)
        {
            App.Popups.Open(PopupType.Pause);
        }
    }

    private void BindCoinEvents()
    {
        if (App.Coins == null)
        {
            return;
        }

        App.Coins.OnCoinsChanged -= HandleCoinsChanged;
        App.Coins.OnCoinsChanged += HandleCoinsChanged;
    }

    private void UnbindCoinEvents()
    {
        if (App.Coins == null)
        {
            return;
        }

        App.Coins.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int totalCoins, int sessionCoins, int deltaCoins)
    {
        SetGoldText(totalCoins);
    }

    private void RefreshGoldText()
    {
        int totalCoins = App.Coins != null ? App.Coins.TotalCoins : 0;
        SetGoldText(totalCoins);
    }

    private void SetGoldText(int totalCoins)
    {
        if (goldText == null)
        {
            return;
        }

        goldText.text = totalCoins.ToString();
    }
}
