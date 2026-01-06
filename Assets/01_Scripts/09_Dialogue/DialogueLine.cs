using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//대화 구조체

[Serializable]
public struct DialogueLine
{
    public string textKey;
  
    public TextEntry Entry => TextManager.Instance.GetEntry(textKey); // TextManager의 GetEntry를 사용하여 데이터 통째로 접근

    public string SpeakerName => Entry != null ? Entry.speaker : "Unknown";
    public string TranslatedContent => TextManager.Instance.GetText(textKey);
}
