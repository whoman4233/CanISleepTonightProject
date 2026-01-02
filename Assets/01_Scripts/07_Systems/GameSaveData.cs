
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int currentDay;
    public GamePhase currentPhase;
    public int riotGauge;
    public int currentHp;

    // 🔥 추가: 스케줄 저장용 리스트 (Dictionary 대신 사용)
    public List<PrisonerSaveData> prisonerRoster = new List<PrisonerSaveData>();
    public List<DailyScheduleSaveData> weeklySchedule = new List<DailyScheduleSaveData>();
}

[System.Serializable]
public class EndingData // 엔딩 수집 정보. 루프해도 유지됨
{
    public List<GameEndingType> unlockedEndings = new List<GameEndingType>();
}

