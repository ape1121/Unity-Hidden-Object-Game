using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class CanvasGroupUserInterface : MonoBehaviour, UserInterface
{
    [Header("Transitions")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float enterDuration = 0.2f;
    [SerializeField, Min(0f)] private float exitDuration = 0.16f;
    [SerializeField] private Ease enterEase = Ease.OutCubic;
    [SerializeField] private Ease exitEase = Ease.InCubic;
    [SerializeField] private bool hideOnExit = false;

    [Header("Actions")]
    [SerializeField] private Button pauseButton;

    private Tween visibilityTween;

    protected virtual void Awake()
    {
        EnsureCanvasGroup();
    }

    protected virtual void OnEnable()
    {
        BindPauseButton();
    }

    protected virtual void OnDisable()
    {
        UnbindPauseButton();
    }

    protected virtual void OnDestroy()
    {
        UnbindPauseButton();
        visibilityTween?.Kill();
    }

    public virtual void Enter(bool instant = false)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null)
        {
            return;
        }

        gameObject.SetActive(true);
        visibilityTween?.Kill();

        if (instant || enterDuration <= 0f)
        {
            SetVisibleState();
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
        visibilityTween = canvasGroup
            .DOFade(1f, enterDuration)
            .SetEase(enterEase)
            .OnComplete(SetVisibleState);
    }

    public virtual void Exit(bool instant = false)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null)
        {
            return;
        }

        visibilityTween?.Kill();

        if (instant || exitDuration <= 0f)
        {
            SetHiddenState();
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        visibilityTween = canvasGroup
            .DOFade(0f, exitDuration)
            .SetEase(exitEase)
            .OnComplete(SetHiddenState);
    }

    private void SetVisibleState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    private void SetHiddenState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (hideOnExit)
        {
            gameObject.SetActive(false);
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
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
        if (App.Sessions == null)
        {
            return;
        }

        if (App.Sessions.State == GameSessionState.Running)
        {
            App.Sessions.PauseSession();
        }
        else if (App.Sessions.State != GameSessionState.Paused)
        {
            return;
        }

        if (App.Popups != null)
        {
            App.Popups.Open(PopupType.Pause);
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        EnsureCanvasGroup();
    }
#endif
}
