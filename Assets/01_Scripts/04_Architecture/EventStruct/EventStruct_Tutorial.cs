using System;
using UnityEngine;
using static TutorialNPC;

//==========================================
// 튜토리얼 관련 이벤트
//==========================================

public struct DialogueStepChangedEvent
{
    public DialogueKeys.DialogueType NewStep;

    public DialogueStepChangedEvent(DialogueKeys.DialogueType step)
    {
        NewStep = step;
    }
}