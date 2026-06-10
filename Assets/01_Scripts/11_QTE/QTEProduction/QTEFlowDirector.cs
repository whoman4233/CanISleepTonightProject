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
    [SerializeField] private QTEVolumeController qteVolumeController;

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

    // ======================================================
    // Filter
    // ======================================================

    private bool PassFilter(QTEActionSO action)
    {
        return actionFilter == null || action == actionFilter;
    }

    // ======================================================
    // QTE Lifecycle
    // ======================================================

    private void OnQTEStarted(QTEStartedEvent e)
    {
        if (!PassFilter(e.Action))
            return;

        // QTE 컨텍스트 초기화
        PrisonerQTEContext.CurrentAction = e.Action;
        PrisonerQTEContext.CurrentResult = default;
        PrisonerQTEContext.DamageConsumed = false;

        // 상세보기 강제 종료
        EventBus.Publish(new ForceExitInspectionEvent());

        // 카메라 QTE 모드 진입
        Transform attacker = PrisonerQTEContext.CurrentAttacker != null
            ? PrisonerQTEContext.CurrentAttacker.transform
            : null;

        if (cameraDirector != null)
            cameraDirector.EnterQTEMode(attacker);

        if (shakeController != null)
            shakeController.OnQTEStarted();

        if (qteVolumeController != null)
            qteVolumeController.Enter(attacker);
    }

    private void OnQTEInputFeedback(QTEInputFeedbackEvent e)
    {
        if (e.State == QTEInputState.Pressed && shakeController != null)
        {
            shakeController.PlayButtonImpulse();
        }
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        if (!PassFilter(e.Action))
            return;

        // 결과만 기록 (애니메이션/데미지는 다른 시스템에서 처리)
        PrisonerQTEContext.CurrentResult = e.Result;

        // 카메라 복귀
        if (cameraDirector != null)
            cameraDirector.ExitQTEMode();

        // 쉐이크 종료
        if (shakeController != null)
            shakeController.OnQTEEnded();

        // DOF 효과 종료
        if (qteVolumeController != null)
            qteVolumeController.Exit();
    }

    // ======================================================
    // Animation Event → Camera Shake
    // ======================================================

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



