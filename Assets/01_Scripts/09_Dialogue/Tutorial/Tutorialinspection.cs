using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorialinspection : InspectableObject
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크

    public override void Interact(Player player)
    {
        base.Interact(player);
        TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (npc == null) return;

        if (!_isTriggered && (int)npc.currentSubStep == (int)stepToPublish - 1)
        {
            EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
            _isTriggered = true;
            Debug.Log("튜토리얼 이벤트 발행 완료");
        }
        else
        {
            Debug.Log("현재 스텝이 맞지 않거나 이벤트가 이미 발행되었습니다");
        }

    }
}
