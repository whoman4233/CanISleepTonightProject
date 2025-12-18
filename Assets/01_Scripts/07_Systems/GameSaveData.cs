
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData // 세이브 데이터에 저장 할 것들
{
    public int currentDay;
    public GamePhase currentPhase;
    public int riotGauge;
}

[System.Serializable]
public class EndingData // 엔딩 수집 정보. 루프해도 유지됨
{
    public List<GameEndingType> unlockedEndings = new List<GameEndingType>();
}