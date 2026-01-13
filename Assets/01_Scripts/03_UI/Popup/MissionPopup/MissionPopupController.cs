using System;
using UnityEngine;

public class MissionPopupController : MonoBehaviour
{
    private Action<MissionBriefingDialogueEndedEvent> _onEnded;

    private void Awake()
    {
        _onEnded = OnEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onEnded);
    }

    private void OnEnded(MissionBriefingDialogueEndedEvent e)
    {
        // UI를 직접 만지지 않고, 표시 요청 이벤트만 발행
        EventBus.Publish(new MissionPopupShowRequestedEvent(e.mission));
    }
}

