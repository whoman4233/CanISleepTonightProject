using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwlInteract : InspectHiddenItemAction
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크

    public override void InspectAction(IInspectable owner)
    {
        Debug.Log("<color=yellow>1. InspectAction 진입함</color>");
        base.InspectAction(owner);
        Debug.Log("<color=cyan>2. 부모 로직 통과함</color>");
        TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (npc == null) return;
        if (!_isTriggered && (int)npc.currentSubStep == (int)stepToPublish - 1)
        {
            EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
            _isTriggered = true;
            System.Action onComplete = null;
            if (npc.currentSubStep == DialogueKeys.DialogueType.BookRead)
            {
                onComplete = () => {
                    Debug.Log("최종 튜토리얼 대화 종료. 플레이 씬으로 이동합니다.");
                    EventBus.Publish(new IntoPlaySceneEvent());
                };
            }
            DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, stepToPublish.ToString(), onComplete);
            Debug.Log("튜토리얼 이벤트 발행 완료");
        }
    }
}
