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
            // 여기가 미미의 대사가 끝나는 분기.
            // 이 괄호 안에서 이벤트발행 혹은 매서드 직접 실행.
            Debug.Log("미미 대사 끝");
        };

        HandleDialogue(onDialogueComplete); // 콜백 전달
    }
}
