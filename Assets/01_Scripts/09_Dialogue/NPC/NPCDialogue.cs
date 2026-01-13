using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour , IInteractable
{
    [Header("NPC 설정")]
    public DialogueKeys.SpeakerType speakerRole;

    private DialogueManager dialogueManager;
    private Action<DialogueStepChangedEvent> _onStepChanged;
    [Header("Current Progress")]
    public DialogueKeys.DialogueType currentSubStep = DialogueKeys.DialogueType.Dialogue;

    private bool finishedDialogue = false;


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

    private void Start()
    {
        if (dialogueManager == null)
            dialogueManager = GameObject.FindAnyObjectByType<DialogueManager>();
    }

    public void Interact(Player player)
    {
        if (finishedDialogue) return;
        string speakerKey = speakerRole.ToString();
        string textType = currentSubStep.ToString();
        dialogueManager.StartDialogueByKeys(speakerKey, textType);
        finishedDialogue = true;
    }

    //private string DetermineTextType()
    //{

    //    // 2. 미션 중일 때 (예: 미션 종료 후 대화)
    //    var mission = DailyMissionManager.Instance.CurrentMission;
    //    //if (mission != null)
    //    //{
    //    //    // (미션 성공/실패 여부에 따라 "Complete", "Fail"로 세분화 가능)
    //    //    if (mission.IsCompleted) return "Fin";
    //    //}

    //    // 3. 기본값
    //    return DialogueKeys.DialogueType.Dialogue.ToString(); // 기본값
    //}

    private void UpdateStep(DialogueKeys.DialogueType nextStep)
    {
        // 미션성공 or 실패일 때(최종결과)에는 단계 진행 무시
        bool isFinalResult = (nextStep == DialogueKeys.DialogueType.Complete || nextStep == DialogueKeys.DialogueType.Fail);

        // 일반적인 단계 진행이거나 최종 결과일 때만 업데이트
        if (isFinalResult || nextStep > currentSubStep)
        {
            currentSubStep = nextStep;
            finishedDialogue = false;
            Debug.Log($"[{gameObject.name}] 상태 업데이트 완료: {nextStep}");
        }
    }
}
