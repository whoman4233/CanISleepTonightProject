using System;
using UnityEngine;
using static TutorialNPC;

//==========================================
// 튜토리얼 관련 이벤트
//==========================================

public struct TutorialStepChangedEvent
{
    public TutorialSubStep NewStep;
    public TutorialStepChangedEvent(TutorialSubStep step) => NewStep = step;
}