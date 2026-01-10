using System;
using UnityEngine;

public class QTEFlowDirector : MonoBehaviour
{
    [Header("Filter")]
    [Tooltip("비어 있으면 모든 QTEStartedEvent를 연출로 처리")]
    [SerializeField] private string qteIdFilter = "PrisonerAttack";

    [Header("Refs")]
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField] private CameraShakeController shakeController;

    private Action<QTEStartedEvent> _onStart;
    private Action<QTEInputFeedbackEvent> _onInput;
    private Action<QTEEndedEvent> _onEnd;

    private Action<PrisonerAttackShakeStartEvent> _onShakeStart;
    private Action<PrisonerAttackShakeEndEvent> _onShakeEnd;

    private void Awake()
    {
        _onStart = OnQTEStarted;
        _onInput = OnQTEInputFeedback;
        _onEnd = OnQTEEnded;

        _onShakeStart = OnShakeStart;
        _onShakeEnd = OnShakeEnd;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onStart);
        EventBus.Subscribe(_onInput);
        EventBus.Subscribe(_onEnd);
        EventBus.Subscribe(_onShakeStart);
        EventBus.Subscribe(_onShakeEnd);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStart);
        EventBus.Unsubscribe(_onInput);
        EventBus.Unsubscribe(_onEnd);
        EventBus.Unsubscribe(_onShakeStart);
        EventBus.Unsubscribe(_onShakeEnd);
    }

    private bool PassFilter(string qteId)
    {
        return string.IsNullOrEmpty(qteIdFilter) || qteId == qteIdFilter;
    }

    // =========================
    // QTE Lifecycle
    // =========================

    private void OnQTEStarted(QTEStartedEvent e)
    {
        if (!PassFilter(e.QTEId))
            return;

        // 상세보기 강제 종료
        EventBus.Publish(new ForceExitInspectionEvent());

        // 카메라 QTE 모드 진입
        Transform attacker = PrisonerQTEContext.CurrentAttacker;
        if (cameraDirector != null)
            cameraDirector.EnterQTEMode(attacker);

        // 죄수 공격 애니메이션 시작
        if (PrisonerQTEContext.CurrentAttackerAnimator != null)
            PrisonerQTEContext.CurrentAttackerAnimator.PlayAttack();

        //  BaseShake는 애니메이션 이벤트(AttackShakeStart)에서만 제어
    }

    private void OnQTEInputFeedback(QTEInputFeedbackEvent e)
    {
        // 버튼 입력 시: 발버둥
        if (e.State == QTEInputState.Pressed && shakeController != null)
        {
            shakeController.PlayButtonImpulse();
        }
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        if (!PassFilter(e.QTEId))
            return;

        // 카메라 원상복귀
        if (cameraDirector != null)
            cameraDirector.ExitQTEMode();

        // 성공했을 때만 죄수가 맞는다
        if (e.Result == QTEResult.Success)
        {
            if (PrisonerQTEContext.CurrentAttackerAnimator != null)
                PrisonerQTEContext.CurrentAttackerAnimator.PlayFail(); // = Hit
        }

        // 모든 흔들림 정리
        if (shakeController != null)
            shakeController.ResetAll();

        PrisonerQTEContext.Clear();
    }

    // =========================
    // Animation Event → Camera
    // =========================

    private void OnShakeStart(PrisonerAttackShakeStartEvent e)
    {
        // 죄수 공격 애니메이션 중 "붙잡힘" 상태 시작
        if (shakeController != null)
            shakeController.StartBaseShake();
    }

    private void OnShakeEnd(PrisonerAttackShakeEndEvent e)
    {
        // 공격 애니메이션 종료
        if (shakeController != null)
            shakeController.StopBaseShake();
    }
}

