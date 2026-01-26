using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimiDialogue : PrisonerDialogue
{
    private bool _isFinished = false; // 미미 이벤트를 단발성으로 유지
    public override void Interact(Player player)
    {
        if(_isFinished) return;
        _isFinished = true;

        System.Action onDialogueComplete = () =>
        {
            EventBus.Publish(new Mission03DialogueEnded());
        };

        HandleDialogue(onDialogueComplete); // 콜백 전달
    }
}
