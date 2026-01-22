using UnityEngine;
using System;

public class PlayerQTEAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Triggers")]
    [SerializeField] private string struggleTrigger = "Struggle";
    [SerializeField] private string failTrigger = "QTEFail";

    private int _struggleHash;
    private int _failHash;
    private bool _qteActive;

    private Action<QTEInputFeedbackEvent> _onInput;
    private Action<QTEStartedEvent> _onStarted;
    private Action<QTEEndedEvent> _onEnded;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _struggleHash = Animator.StringToHash(struggleTrigger);
        _failHash = Animator.StringToHash(failTrigger);

        _onInput = OnInput;
        _onStarted = OnStarted;
        _onEnded = OnEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onStarted);
        EventBus.Subscribe(_onInput);
        EventBus.Subscribe(_onEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStarted);
        EventBus.Unsubscribe(_onInput);
        EventBus.Unsubscribe(_onEnded);
    }
    private void OnStarted(QTEStartedEvent e)
    {
        _qteActive = true;
    }
    // =========================
    // Input Feedback (연타/버튼)
    // =========================

    private void OnInput(QTEInputFeedbackEvent e)
    {
        if (!_qteActive)
            return;

        if (e.State != QTEInputState.Pressed)
            return;

        animator.SetTrigger(_struggleHash);
    }

    // =========================
    // QTE Result
    // =========================

    private void OnEnded(QTEEndedEvent e)
    {
        _qteActive = false;

        animator.ResetTrigger(_struggleHash);
        animator.ResetTrigger(_failHash);

        if (e.Result == QTEResult.Fail || e.Result == QTEResult.Timeout)
        {
            animator.SetTrigger(_failHash);
            return;
        }

        animator.SetTrigger("QTEEnd");
    }


}
