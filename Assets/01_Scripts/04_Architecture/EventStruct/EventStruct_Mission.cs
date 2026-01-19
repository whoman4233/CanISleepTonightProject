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

public struct ForceMissionFailRequestedEvent // 강제 미션 종료(ex:진짜 프랭크 찾기 미션)
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
