using System;
using UnityEngine;

public class MissionPopupRootController : MonoBehaviour
{
    private Action<MissionBriefingDialogueEndedEvent> _onBriefingEnded;
    private Action<SettlementStartedEvent> _onSettlementStarted;
    private Action<UIHardResetEvent> _onUIHardReset;

    private bool _popupShown;

    private void Awake()
    {
        _popupShown = false;

        _onBriefingEnded = OnMissionBriefingDialogueEnded;
        _onSettlementStarted = OnSettlementStarted;
        _onUIHardReset = OnUIHardReset;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onBriefingEnded);
        EventBus.Subscribe(_onSettlementStarted);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onBriefingEnded);
        EventBus.Unsubscribe(_onSettlementStarted);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    // =========================
    // 브리핑 대화 종료 → MissionPopup 표시
    // =========================
    private void OnMissionBriefingDialogueEnded(MissionBriefingDialogueEndedEvent e)
    {
        if (_popupShown)
            return;

        _popupShown = true;

        EventBus.Publish(
            new MissionPopupShowRequestedEvent(
                DailyMissionManager.Instance.CurrentMission
            )
        );

        LockInput();
    }

    // =========================
    // 정산 시작 시 Result UI 진입
    // =========================
    private void OnSettlementStarted(SettlementStartedEvent e)
    {
        LockInput();
        EventBus.Publish(new ResultUIShowRequestedEvent());
    }


    // =========================
    // UIHardReset 시 MissionPopup 상태 복구
    // =========================
    private void OnUIHardReset(UIHardResetEvent e)
    {
        // 씬 리로딩 / 타이틀 복귀 시
        // MissionPopup은 다시 표시 가능해야 함
        _popupShown = false;

        UnlockInput();
    }

    // =========================
    // Input Lock Helpers
    // =========================
    private void LockInput()
    {
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    private void UnlockInput()
    {
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }
}






