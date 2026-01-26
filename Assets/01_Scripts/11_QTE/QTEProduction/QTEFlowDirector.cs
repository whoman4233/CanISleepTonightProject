using System;
using UnityEngine;

public class QTEFlowDirector : MonoBehaviour
{
    [Header("Filter")]
    [Tooltip("비어 있으면 모든 QTEStartedEvent를 연출로 처리")]
    [SerializeField] private QTEActionSO actionFilter;

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

    // =========================
    // Filter
    // =========================

    private bool PassFilter(QTEActionSO action)
    {
        return actionFilter == null || action == actionFilter;
    }

    // =========================
    // QTE Lifecycle
    // =========================

    private void OnQTEStarted(QTEStartedEvent e)
    {
        if (!PassFilter(e.Action))
            return;

        // --------------------------------------------------
        // ★ QTE 컨텍스트 초기화 (중복 데미지 방지의 시작점)
        // --------------------------------------------------
        PrisonerQTEContext.CurrentAction = e.Action;
        PrisonerQTEContext.CurrentResult = default;
        PrisonerQTEContext.DamageConsumed = false;

        // 상세보기 강제 종료
        EventBus.Publish(new ForceExitInspectionEvent());

        // 카메라 QTE 모드 진입
        GameObject attackerGO = PrisonerQTEContext.CurrentAttacker;
        Transform attacker = attackerGO != null ? attackerGO.transform : null;

        if (cameraDirector != null)
            cameraDirector.EnterQTEMode(attacker);
        if (shakeController != null)
            shakeController.OnQTEStarted();

        // 죄수 공격 애니메이션 시작 (★ Animator가 아니라 PrisonerQTEAnimator에 호출해야 함)
        if (PrisonerQTEContext.CurrentAttackerQTEAnimator != null)
            PrisonerQTEContext.CurrentAttackerQTEAnimator.PlayAttackFail();

        // BaseShake는 애니메이션 이벤트에서 제어
    }

    private void OnQTEInputFeedback(QTEInputFeedbackEvent e)
    {
        // 버튼 입력 시 발버둥 연출
        if (e.State == QTEInputState.Pressed && shakeController != null)
        {
            shakeController.PlayButtonImpulse();
        }
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        if (!PassFilter(e.Action))
            return;

        // --------------------------------------------------
        // ★ 결과만 기록 (데미지는 Animation Event 타이밍에 Resolver가 처리)
        // --------------------------------------------------
        PrisonerQTEContext.CurrentResult = e.Result;

        // 카메라 원상 복귀
        if (cameraDirector != null)
            cameraDirector.ExitQTEMode();

        // 성공 시 죄수 피격 연출 (★ 이것도 PrisonerQTEAnimator 메서드)
        if (e.Result == QTEResult.Success)
        {
            if (PrisonerQTEContext.CurrentAttackerQTEAnimator != null)
                PrisonerQTEContext.CurrentAttackerQTEAnimator.PlayHitSuccess();
        }

        // 흔들림 정리
        if (shakeController != null)
            shakeController.OnQTEEnded();

        // ★ 주의: 여기서 Clear() 하면 애니메이션 이벤트 타이밍이 뒤늦게 올 때 문제
        // PrisonerQTEContext.Clear();
        // -> Clear는 Resolver가 데미지 1회 소비 후 호출하는 편이 안전
    }

    // =========================
    // Animation Event → Camera
    // =========================

    private void OnShakeStart(PrisonerAttackShakeStartEvent e)
    {
        if (shakeController != null)
            shakeController.StartBaseShake();
    }

    private void OnShakeEnd(PrisonerAttackShakeEndEvent e)
    {
        if (shakeController != null)
            shakeController.StopBaseShake();
    }
}






