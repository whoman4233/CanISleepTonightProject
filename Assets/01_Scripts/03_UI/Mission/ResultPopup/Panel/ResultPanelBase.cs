using System;
using UnityEngine;

public abstract class ResultPanelBase : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] protected GameObject root;

    [Header("Stamp")]
    [SerializeField] protected Animator stampAnimator;

    [Header("Buttons")]
    [SerializeField] protected CanvasGroup buttonsGroup;

    [Header("UISound")]
    [SerializeField] private AudioClip stampSound;

    private bool _waitingForStampClick;
    private bool _stampPlayed;

    // =========================
    // EventHandler
    // =========================
    private Action<UIProceedRequestedEvent> _onUIProceedRequested;
    private Action<UIHardResetEvent> _onUIHardReset;
    protected virtual void Awake()
    {
        DisableButtons();

        _onUIProceedRequested = OnUIProceedRequested;
        _onUIHardReset = OnUIHardReset;
    }

    protected virtual void OnEnable()
    {
        EventBus.Subscribe(_onUIProceedRequested);
        EventBus.Subscribe(_onUIHardReset);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe(_onUIProceedRequested);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    public virtual void Show()
    {
        root.SetActive(true);
        _waitingForStampClick = true;
        _stampPlayed = false;
        DisableButtons();
    }

    public virtual void Hide()
    {
        root.SetActive(false);
    }

    // =========================
    // 이벤트 핸들러
    // =========================
    private void OnUIProceedRequested(UIProceedRequestedEvent e)
    {
        if (!_waitingForStampClick || _stampPlayed)
            return;

        PlayStamp();
    }

    private void PlayStamp()
    {
        _stampPlayed = true;
        _waitingForStampClick = false;

        if (stampAnimator != null)
            stampAnimator.SetTrigger("Stamp");
        AudioManager.Instance.PlayUISound(stampSound);
    }

    // Animation Event
    public void OnStampAnimationFinished()
    {
        EnableButtons();
    }

    private void DisableButtons()
    {
        if (buttonsGroup == null)
            return;

        buttonsGroup.alpha = 0f;
        buttonsGroup.blocksRaycasts = false;
        buttonsGroup.interactable = false;
    }

    private void EnableButtons()
    {
        if (buttonsGroup == null)
            return;

        buttonsGroup.alpha = 1f;
        buttonsGroup.blocksRaycasts = true;
        buttonsGroup.interactable = true;
    }
    // =========================
    // UI Hard Reset 처리
    // =========================
    private void OnUIHardReset(UIHardResetEvent e)
    {
        _waitingForStampClick = false;
        _stampPlayed = false;

        Hide();
    }
}


