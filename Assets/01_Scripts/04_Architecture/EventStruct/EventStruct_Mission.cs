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
