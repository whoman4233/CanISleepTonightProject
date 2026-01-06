using System.Collections.Generic;


[System.Serializable]
public class GameSaveData
{
    public int currentDay;
    public int riotGauge;
    public GamePhase currentPhase;
    public int currentHp;

    // [1] 거주자 명부 (체력, 억압됨 여부 등 영구 데이터)
    public List<PrisonerSaveData> prisonerRoster = new List<PrisonerSaveData>();

    // 🔥 [변경] 7일치 스케줄(WeeklySchedule) 삭제 -> 오늘 배역(DailyRoles) 추가
    // (저장 시점의 '누가 범인인가' 상태를 저장)
    public List<DailyRoleSaveData> dailyRoles = new List<DailyRoleSaveData>();
}
[System.Serializable]
public class EndingData // 엔딩 수집 정보. 루프해도 유지됨
{
    public List<GameEndingType> unlockedEndings = new List<GameEndingType>();
}

