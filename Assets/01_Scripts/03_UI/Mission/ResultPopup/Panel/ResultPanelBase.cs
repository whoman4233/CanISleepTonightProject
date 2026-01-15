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

    private bool _waitingForStampClick;
    private bool _stampPlayed;

    // =========================
    // EventHandler
    // =========================
    private Action<UIProceedRequestedEvent> _onUIProceedRequested;

    protected virtual void Awake()
    {
        DisableButtons();

        _onUIProceedRequested = OnUIProceedRequested;
    }

    protected virtual void OnEnable()
    {
        EventBus.Subscribe(_onUIProceedRequested);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe(_onUIProceedRequested);
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
}


