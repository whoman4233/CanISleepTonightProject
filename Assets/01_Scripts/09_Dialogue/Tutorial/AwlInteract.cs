using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwlInteract : InspectHiddenItemAction
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크

    public override void InspectAction(IInspectable owner)
    {
        if (_isTriggered) return;
        TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (npc == null) return;
        if (!_isTriggered && (int)npc.currentSubStep == (int)stepToPublish - 2) // bookread스텝 건너뛰고 바로 close스텝으로 전환
        {
            _isTriggered = true;
            StartCoroutine(DelayedSequence(owner));

            Debug.Log("튜토리얼 이벤트 발행 완료");
            return;
        }

    }

    private IEnumerator DelayedSequence(IInspectable owner)
    {
        EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));

        yield return new WaitForEndOfFrame();

        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, DialogueKeys.Types.BookRead);
        yield return null;

        base.InspectAction(owner);
    }
}
