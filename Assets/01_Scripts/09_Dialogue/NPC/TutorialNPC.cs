using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialNPC : MonoBehaviour , IInteractable
{   
    [Header("NPC 설정")]
    public DialogueKeys.SpeakerType speakerRole = DialogueKeys.SpeakerType.Frank;

    [Header("Current Progress")]
    public DialogueKeys.DialogueType currentSubStep = DialogueKeys.DialogueType.Dialogue;

    //[Header("Current Progress")]
    //public TutorialSubStep currentSubStep = TutorialSubStep.Basic;

    private Action<DialogueStepChangedEvent> _onStepChanged;

    //[Header("대화 데이터")]
    //[SerializeField] private DialogueManager dialogueManager;

    private bool finishedDialogue = false; // 대사 다 봄?

    private void Awake()
    {
        _onStepChanged = e =>
        {
            UpdateStep(e.NewStep);
        };
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onStepChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStepChanged);
    }
    public void Interact(Player player)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Tutorial) return; // 튜토리얼 페이즈에서만 실행
        if (finishedDialogue) return;

        var dialogueManager = DialogueManager.Instance; //DialogueManager 전역접근
        if (dialogueManager == null) 
            return;

        string speakerKey = speakerRole.ToString();
        string textType = currentSubStep.ToString();
        dialogueManager.StartDialogueByKeys(speakerKey, textType);
        finishedDialogue = true;
    }

    public void OnAttacked()
    {
        if (currentSubStep == DialogueKeys.DialogueType.BatonEquipped)
        {
            EventBus.Publish(new DialogueStepChangedEvent(DialogueKeys.DialogueType.NPCHit));
            Debug.Log("이벤트 발행: NPCHit");
            StartCoroutine(DelayedDialogue());
            finishedDialogue = true;
        }
    }

    private void UpdateStep(DialogueKeys.DialogueType nextStep)
    {
        // 단계가 역행하지 않도록 체크
        if (nextStep > currentSubStep)
        {
            currentSubStep = nextStep;
            finishedDialogue = false;
            Debug.Log($"튜토리얼 스텝이 업데이트 되었읍니다. {nextStep}");
        }
    }

    private IEnumerator DelayedDialogue() // 때린 후 대화생성 딜레이를 주기 위한 코루틴
    {
        yield return new WaitForSeconds(0.5f);
        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, currentSubStep.ToString());
    }
}
