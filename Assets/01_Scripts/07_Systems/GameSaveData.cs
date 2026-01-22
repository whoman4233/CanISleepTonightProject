using System.Collections.Generic;


[System.Serializable]
public class GameSaveData
{
    public int currentDay;
    public GamePhase currentPhase;
    public int currentHp;

    public int currentMissionIndex;
    public bool isMissionInProgress;

    // [1] 거주자 명부 (체력, 억압됨 여부 등 영구 데이터)
    public List<PrisonerSaveData> prisonerRoster = new List<PrisonerSaveData>();

    // (저장 시점의 '누가 범인인가' 상태를 저장)
    public List<DailyRoleSaveData> dailyRoles = new List<DailyRoleSaveData>();

    // 섞인 미션 순서 저장용 (로드 시 복구하기 위함)
    public List<int> randomizedMissionIndices = new List<int>();
}
[System.Serializable]
public class EndingData // 엔딩 수집 정보. 루프해도 유지됨
{
    public List<GameEndingType> unlockedEndings = new List<GameEndingType>();
}

