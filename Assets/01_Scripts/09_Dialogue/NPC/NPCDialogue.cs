using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour , IInteractable
{
    [Header("NPC 설정")]
    public DialogueKeys.SpeakerType speakerRole;

    private DialogueManager _dialogueManager;

    private void Start()
    {
        if (_dialogueManager == null)
            _dialogueManager = GameObject.FindAnyObjectByType<DialogueManager>();
    }

    public void Interact(Player player)
    {
        if (_dialogueManager == null) return;

        string speakerKey = speakerRole.ToString(); // enum을 문자열로 변경
        string textType = DetermineTextType();

        _dialogueManager.StartDialogueByKeys(speakerKey, textType);
    }

    private string DetermineTextType()
    {

        // 2. 미션 중일 때 (예: 미션 종료 후 대화)
        var mission = DailyMissionManager.Instance.CurrentMission;
        //if (mission != null)
        //{
        //    // (미션 성공/실패 여부에 따라 "Complete", "Fail"로 세분화 가능)
        //    if (mission.IsCompleted) return "Fin";
        //}

        // 3. 기본값
        return DialogueKeys.DialogueType.Dialogue.ToString(); // 기본값
    }
}
