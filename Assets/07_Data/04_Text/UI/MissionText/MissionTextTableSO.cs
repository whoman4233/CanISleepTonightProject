using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Mission Text Table")]
public class MissionTextTableSO : ScriptableObject
{
    public List<MissionTextSet> missionTextSets;
}

[Serializable]
public class MissionTextSet
{
    public MissionDayTheme theme;
    public int missionIndex; // Day or MissionNumber
    public List<MissionTextEntry> texts;
}

[Serializable]
public class MissionTextEntry
{
    public string id;    // MissionText 컬럼 ID
    public string text;
    public string info;
}
