using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialNPC;


public class TutorialCarryEventTrigger : CarryableBox
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크
    [SerializeField] private TutorialNPC _cachedNpc;

    public override void Interact(Player player) // 그냥 오브젝트에 상호작용하면 바로 스텝 넘어가게
    {
        TutorialOutLiner.Instance.StopCurrentHighlight();
        //if (_cachedNpc == null) _cachedNpc = FindObjectOfType<TutorialNPC>();

        if (_cachedNpc != null && _cachedNpc.currentSubStep == DialogueKeys.DialogueType.BoardSee)
        {
            base.Interact(player);
        }
    }

    public override void Drop(Player player)
    {
        if (_isTriggered) return;
        //if (_cachedNpc == null) _cachedNpc = FindObjectOfType<TutorialNPC>();

        if (_cachedNpc != null && !_isTriggered)
        {
            if ((int)_cachedNpc.currentSubStep == (int)stepToPublish - 1)
            {
                _isTriggered = true;
                EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
                StartCoroutine(DelayedDialogueStart());
                TutorialOutLiner.Instance.UpdateHighlight(DialogueKeys.DialogueType.BoxOpened);
                _cachedNpc.finishedDialogue = true;
                Debug.Log("[BoxDrop]튜토리얼 미션 UI 갱신 성공!!");
            }
        }
        base.Drop(player);
    }
    private IEnumerator DelayedDialogueStart()
    {
        yield return null;
        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, stepToPublish.ToString());
        Debug.Log("[Box] UI 갱신 완료");
    }
}
