using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//대화 구조체

[Serializable]
public struct DialogueLine
{
    [SerializeField] private string speakerKey; // npc이름
    [SerializeField] private string textKey;    // 대화내용

    // TextManager를 통해 번역된 텍스트를 반환
    public string SpeakerName => TextManager.Instance.GetText(speakerKey);
    public string Text => TextManager.Instance.GetText(textKey);
}
