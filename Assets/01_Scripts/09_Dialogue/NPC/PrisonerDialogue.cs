using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerDialogue : MonoBehaviour , IInteractable 
{
    public VisualAnomalyType myVisualType;
    private string _mySpeakerKey = null; // 기본값은 null
    private DialogueManager dialogueManager;

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = GameObject.FindAnyObjectByType<DialogueManager>();
        }
        InitializeIdentity();
    }
    public void InitializeIdentity()
    {
        // 용의자들만 대사 키를 할당받음
        switch (myVisualType)
        {
            case VisualAnomalyType.Suspect1:
                _mySpeakerKey = DialogueKeys.Speakers.Suspect1;
                break;
            case VisualAnomalyType.Suspect2:
                _mySpeakerKey = DialogueKeys.Speakers.Suspect2;
                break;
            case VisualAnomalyType.Suspect3:
                _mySpeakerKey = DialogueKeys.Speakers.Suspect3;
                break;
            default:
                _mySpeakerKey = null; // 그 외 일반 죄수는 키 없음
                break;
        }
    }

    public void Interact(Player player)
    {
        if (string.IsNullOrEmpty(_mySpeakerKey))
        {
            Debug.Log($"{gameObject.name}: 대사 데이터가 없는 일반 죄수입니다.");
            return;
        }

        dialogueManager.StartDialogueByKeys(_mySpeakerKey);
    }
}
