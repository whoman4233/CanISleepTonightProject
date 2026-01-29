using UnityEngine;
using System;

public class PlayerQTEAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("QTE Animator Params")]
    [SerializeField] private string struggleTrigger = "Struggle"; // Guarding 재생용
    [SerializeField] private string failTrigger = "QTEFail";      // GuardFail 재생용
    [SerializeField] private string guardingBool = "IsGuarding";

    [Header("QTELayer Index")]
    [SerializeField] private int qteLayerIndex = 2; // ← Animator에서 QTELayer 인덱스와 반드시 일치

    [Header("QTE SFX")]
    [SerializeField] private AudioClip inputSfx;
    [SerializeField] private AudioClip guardFailSfx;

    // 현재 플레이어에게 걸린 QTE 액션
    private QTEActionSO _myAction;

    private int _struggleHash;
    private int _failHash;

    private bool _qteActive;
    private bool _guardingStarted;

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

    // ======================================================
    // QTE 시작
    // ======================================================
    private void OnStarted(QTEStartedEvent e)
    {
        _qteActive = true;
        _guardingStarted = false;

        _myAction = e.Action;

        // QTE Layer 활성화
        animator.SetLayerWeight(qteLayerIndex, 1f);

        // 혹시 남아있을 수 있는 트리거 정리
        animator.ResetTrigger(_struggleHash);
        animator.ResetTrigger(_failHash);
    }

    // ======================================================
    // QTE 입력 피드백 (버튼 연타)
    // ======================================================
    private void OnInput(QTEInputFeedbackEvent e)
    {
        if (!_qteActive || e.State != QTEInputState.Pressed)
            return;

        // 입력 SFX (매 입력)
        if (inputSfx != null)
            AudioManager.Instance?.PlaySFX(inputSfx);

        // 첫 입력에만 Guarding 진입
        if (_guardingStarted)
            return;

        _guardingStarted = true;
        animator.SetTrigger(_struggleHash);
    }

    // ======================================================
    // QTE 종료
    // ======================================================
    private void OnEnded(QTEEndedEvent e)
    {
        if (!_qteActive)
            return;

        _qteActive = false;

        // 입력 트리거 정리
        animator.ResetTrigger(_struggleHash);

        // [실패일 때만] GuardFail 1회 재생
        if (e.Result == QTEResult.Fail || e.Result == QTEResult.Timeout)
        {
            animator.SetTrigger(_failHash);
        }

        // GuardFail 클립 마지막에 Animation Event로 호출
        // 성공인 경우는 즉시 복귀해도 문제 없음
        else
        {
            DisableQTELayer();
        }
    }

    // ======================================================
    // Animation Events
    // ======================================================
    public void OnPlayerHitFrame()
    {
        if (_myAction == null)
            return;

        EventBus.Publish(new PlayerHitTimingEvent
        {
            Action = _myAction
        });
    }

    // GuardFail 애니메이션 마지막 프레임에 연결
    public void OnGuardFailFinished()
    {
        DisableQTELayer();
    }
    public void OnGuardFailSfx()
    {
        if (guardFailSfx != null)
            AudioManager.Instance?.PlaySFX(guardFailSfx);
    }
    // ======================================================
    // Internal
    // ======================================================
    private void DisableQTELayer()
    {
        // QTE Layer 비활성화 → Base Layer 상태 그대로 노출
        animator.SetLayerWeight(qteLayerIndex, 0f);

        // 혹시 모를 잔여 트리거 정리
        animator.ResetTrigger(_failHash);
        _myAction = null;
    }
}
