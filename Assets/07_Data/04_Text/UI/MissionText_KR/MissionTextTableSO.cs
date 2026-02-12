using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionTextRole
{
    MissionTitle,        // 미션 제목
    MissionInfo,         // 미션 설명 / 브리핑 본문
    MissionCondition,    // 완료 조건
    MissionProgress,     // 진행 중 안내 문구
    MissionFailReason    // 실패 사유
}

[CreateAssetMenu(menuName = "Data/Mission Text Table")]
public class MissionTextTableSO : ScriptableObject
{
    public List<MissionTextSet> missionTextSets;
}

[Serializable]
public class MissionTextSet
{
    public int missionIndex; // MissionNo / Day
    public List<MissionTextEntry> texts;
}

[Serializable]
public class MissionTextEntry
{
    public MissionTextRole role; // 텍스트 위치
    [TextArea]
    public string text;

#if UNITY_EDITOR
    [TextArea]
    public string info; // [Editor 전용] 참고 메모
#endif
}
