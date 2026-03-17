using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialNPC;

public class TutorialEventTrigger : MonoBehaviour , IInteractable
{
    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크
    [SerializeField] private TutorialNPC _cachedNpc;

    [Header("SFX")]
    [SerializeField] private AudioClip takeClip;

    public void Interact(Player player) // 그냥 오브젝트에 상호작용하면 바로 스텝 넘어가게
    {
        TutorialOutLiner.Instance.StopCurrentHighlight();
        //TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (_cachedNpc == null) return;

        if (!_isTriggered && (int)_cachedNpc.currentSubStep == (int)stepToPublish - 1)
        {
            EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
            _isTriggered = true;
            AudioManager.Instance.PlaySFX(takeClip);
            StartCoroutine(DelayedDialogueStart(stepToPublish.ToString(), _cachedNpc));
            Debug.Log("[Baton] 튜토리얼 이벤트 발행 완료");
        }
        else
        {
            Debug.Log("현재 스텝이 맞지 않거나 이벤트가 이미 발행되었습니다");
        }
    }
    private IEnumerator DelayedDialogueStart(string dialogueKey, TutorialNPC npc)
    {
        yield return null;

        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, dialogueKey);
        npc.finishedDialogue = true;
        yield return null;
        Destroy(gameObject);

    }
}
// 튜토리얼씬의 모든 오브젝트의 컴포넌트에 달아 줄 스크립트
// 상호작용 최초 1회 시 이벤트 발행하여 다음 스텝으로 넘어가게 함.
