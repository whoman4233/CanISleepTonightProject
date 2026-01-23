using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwlInteract : InspectHiddenItemAction
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크

    public override void InspectAction(IInspectable owner)
    {
        TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (npc == null) return;
        if (!_isTriggered && (int)npc.currentSubStep == (int)stepToPublish - 2) // bookread스텝 건너뛰고 바로 close스텝으로 전환
        {
            EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
            _isTriggered = true;
            //EventBus.Publish(new DialogueStepChangedEvent(DialogueKeys.DialogueType.BookClose));
            DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, DialogueKeys.Types.BookRead);
            Debug.Log("튜토리얼 이벤트 발행 완료");
       }
        base.InspectAction(owner);
    }
}
