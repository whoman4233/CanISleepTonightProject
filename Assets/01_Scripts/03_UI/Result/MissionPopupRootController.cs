using System;
using UnityEngine;

public class MissionPopupRootController : MonoBehaviour
{
    private Action<MissionBriefingDialogueEndedEvent> _onBriefingEnded;
    private Action<SettlementStartedEvent> _onSettlementStarted;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onBriefingEnded = e =>
        {
            EventBus.Publish(new MissionPopupShowRequestedEvent());
            LockInput();
        };

        _onSettlementStarted = e =>
        {
            EventBus.Publish(new ResultUIShowRequestedEvent(
                false, string.Empty
            ));
            LockInput();
        };

        _onUIHardReset = e =>
        {
            UnlockInput();
        };
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





