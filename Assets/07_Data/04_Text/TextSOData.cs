using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    Korean,
    English
}

// 인스펙터에서 입력할 데이터 단위 (직렬화 필수)
[Serializable]
public class TextEntry
{
    public string key;          // 검색용 고유 키 (예: "ui_start_btn")
    public string speaker;
    public string mission;
    public string type;
    [TextArea] public string ko; // 한국어 텍스트
    [TextArea] public string en; // 영어 텍스트
}

[CreateAssetMenu(fileName = "TextData", menuName = "Data/TextSOData")]
public class TextSOData : ScriptableObject
{
    [Header("텍스트 데이터 목록")]
    public List<TextEntry> textList = new List<TextEntry>();
}