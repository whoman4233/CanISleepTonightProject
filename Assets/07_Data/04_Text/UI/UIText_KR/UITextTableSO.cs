using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/UI Text Table")]
public class UITextTableSO : ScriptableObject
{
    public List<UITextEntry> entries = new List<UITextEntry>();
}

[Serializable]
public class UITextEntry
{
    public string id;
    [TextArea(3, 10)] // 줄바꿈용으로 추가
    public string text;
    public string info;
}
