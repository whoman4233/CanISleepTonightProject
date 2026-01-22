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

    private Action<QTEInputFeedbackEvent> _onInput;
    private Action<QTEEndedEvent> _onEnded;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _struggleHash = Animator.StringToHash(struggleTrigger);
        _failHash = Animator.StringToHash(failTrigger);

        _onInput = OnInput;
        _onEnded = OnEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onInput);
        EventBus.Subscribe(_onEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onInput);
        EventBus.Unsubscribe(_onEnded);
    }

    // =========================
    // Input Feedback (연타/버튼)
    // =========================

    private void OnInput(QTEInputFeedbackEvent e)
    {
        if (e.State != QTEInputState.Pressed)
            return;

        animator.SetTrigger(_struggleHash);
    }

    // =========================
    // QTE Result
    // =========================

    private void OnEnded(QTEEndedEvent e)
    {
        if (e.Result != QTEResult.Fail)
            return;

        animator.SetTrigger(_failHash);
    }
}
