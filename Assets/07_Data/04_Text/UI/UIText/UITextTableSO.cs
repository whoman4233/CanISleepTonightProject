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
    public string text;
    public string info;
}
