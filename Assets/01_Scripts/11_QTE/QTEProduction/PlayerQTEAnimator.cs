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
    // Input Feedback
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

    // =========================
    // Animation Events (연출 전용)
    // =========================

    // 예: 몸을 강하게 버티는 프레임
    public void OnStrugglePeak()
    {
        // 카메라 흔들림, 숨소리, UI 강조 등
        EventBus.Publish(new PlayerAttackTimingEvent());
    }

    // 예: 실패 시 맞는 순간
    public void OnPlayerHitFrame()
    {
        // 피격 이펙트, 사운드
    }
}

