using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialNPC : MonoBehaviour , IInteractable
{
    public enum TutorialSubStep
    {
        Basic,          // 01 ~ 16 (기본 대사)
        BoxOpened,      // 17 ~ 18 (상자 상호작용 후)
        BatonEquipped,  // 19 ~ 20 (진압봉 획득 후)
        NPCHit,         // 21 ~ 26 (공격 당한 후)
        BookRead,       // 27 ~ 끝 (책 상호작용 후)
        Completed       // 튜토리얼 종료
    }

    [Header("Current Progress")]
    public TutorialSubStep currentSubStep = TutorialSubStep.Basic;

    private Action<TutorialStepChangedEvent> _onStepChanged;

    [Header("대화 데이터")]
    [SerializeField] private DialogueData step1_Basic;     // 01~16
    [SerializeField] private DialogueData step2_Box;       // 17~18
    [SerializeField] private DialogueData step3_Baton;     // 19~20
    [SerializeField] private DialogueData step4_Hit;       // 21~26
    [SerializeField] private DialogueData step5_Book;      // 27~마지막
    [SerializeField] private DialogueManager dialogueManager;

    private void Awake()
    {
        _onStepChanged = e =>
        {
            UpdateStep(e.NewStep);
        };
    }

    private void OnEnable() => EventBus.Subscribe(_onStepChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onStepChanged);
    public void Interact(Player player)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Tutorial) return; // 튜토리얼 페이즈에서만 실행

        switch (currentSubStep) // 스텝에 맞는 대사 출력
        {
            case TutorialSubStep.Basic:
                dialogueManager.StartDialogue(step1_Basic);
                break;
            case TutorialSubStep.BoxOpened:
                dialogueManager.StartDialogue(step2_Box);
                break;
            case TutorialSubStep.BatonEquipped:
                dialogueManager.StartDialogue(step3_Baton);
                break;
            case TutorialSubStep.NPCHit:
                dialogueManager.StartDialogue(step4_Hit);
                break;
            case TutorialSubStep.BookRead:
                dialogueManager.StartDialogue(step5_Book);
                break;
        }
    }

    private void UpdateStep(TutorialSubStep nextStep)
    {
        // 단계가 역행하지 않도록 체크
        if (nextStep > currentSubStep)
        {
            currentSubStep = nextStep;
            Debug.Log($"튜토리얼 스텝이 업데이트 되었읍니다. {nextStep}");
        }
    }
}
