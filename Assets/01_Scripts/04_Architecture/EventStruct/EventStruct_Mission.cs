//==========================================
//Mission 관련 이벤트 목록
//==========================================
public struct MissionStartedEvent
{
    public DailyMissionStrategy mission;
}

public struct MissionProgressChangedEvent
{
    public int current;
    public int target;
}

public struct MissionCompletedEvent
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
