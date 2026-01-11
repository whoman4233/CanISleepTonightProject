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

        _dialogueManager.StartDialogueByKeys(speakerKey);
    }
}
