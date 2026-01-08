using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Mission06Data", menuName = "Mission/M06 Data")]
public class Mission06Data : ScriptableObject
{
    [Header("SuspectName Data")]
    public string Suspect1Name;
    public string Suspect2Name;
    public string Suspect3Name;

    public void Setup(string s1, string s2, string s3)
    {
        Suspect1Name = s1;
        Suspect2Name = s2;
        Suspect3Name = s3;
    }

    public string ProcessText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        return rawText.Replace("Suspect1", Suspect1Name)
                      .Replace("Suspect2", Suspect2Name)
                      .Replace("Suspect3", Suspect3Name);
    }
}
