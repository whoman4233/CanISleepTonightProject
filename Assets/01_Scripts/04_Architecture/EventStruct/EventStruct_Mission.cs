//==========================================
//Mission 관련 이벤트 목록
//==========================================
using System.Collections.Generic;

public struct MissionStartedEvent
{
    public DailyMissionStrategy mission;
}

public struct MissionProgressChangedEvent
{
    public int current;
    public int target;
}

// ==========================================
// 연출 종료 후 미션 종료 요청
// ==========================================
public struct MissionEndRequestedEvent
{
    public bool IsSuccess;
    public MissionEndRequestedEvent(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }
}

public struct MissionFailDialogueRequestedEvent // 미션 04 진짜 프랭크 타격 시 다이얼로그 출력용 이벤트
{

}
public struct MissionAutoReportRequestedEvent // 미션 자동 보고용 이벤트(미션 04)
{

}
//==========================================
//Mission 숨겨진 아이템 관련 이벤트(SFX 재생용)
//==========================================
public struct HiddenItemFoundEvent
{
    public HiddenItemDefinitionSO ItemDefinition;

    public HiddenItemFoundEvent(HiddenItemDefinitionSO itemDefinition)
    {
        ItemDefinition = itemDefinition;
    }
}
public struct Mission4DialogueTriggerSpawnEvent // 미션 04 대화 트리거스폰용 이벤트
{
    public List<Mission4DialogueTrigger> Triggers; 
}


//==========================================
//Mission03 관련 이벤트 목록
//==========================================
public struct Mission03DialogueEnded
{

}

//==========================================
//Mission06 관련 이벤트 목록
//==========================================
public struct Mission06PuzzleShowRequestedEvent // 패널/버튼 노출 이벤트
{
}
public struct Mission06SuspectSelectedEvent //범인 선택 이벤트
{
    public int selectedIndex; // 0,1,2
}
