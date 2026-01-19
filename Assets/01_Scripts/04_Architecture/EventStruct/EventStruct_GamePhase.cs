using System;
using UnityEngine;

//==========================================
//GamePhase관련 이벤트 목록
//==========================================

public struct GamePhaseChangedEvent //게임 페이즈에 따른 UI 변경
{
    public GamePhase Phase;

    public GamePhaseChangedEvent(GamePhase phase)
    {
        Phase = phase;
    }
}

public struct RequestPhaseChangeEvent // 페이즈 변경 요청
{
    public GamePhase TargetPhase;
    public RequestPhaseChangeEvent(GamePhase phase) => TargetPhase = phase;
}

public struct EndingConditionMetEvent // 엔딩 조건 달성 알림
{
    public GameEndingType EndingType;
    public EndingConditionMetEvent(GameEndingType type) => EndingType = type;
}
public struct SettlementStartedEvent // 정산페이즈 시작알림
{

}
public struct SettlementCompletedEvent // 정산페이즈 종료알림
{
    public bool IsEnding;
}

public struct SettlementConfirmedEvent // UI에서 사용할 정산페이즈 확인 이벤트
{

}

public struct RequestSceneReloadEvent // 씬 재로딩 요청 이벤트
{

}
public struct RequestGameRestartEvent //게임 재시작 이벤트
{

}

public struct EndingUIShowRequestedEvent
{
    public EndingUIData Data;
    public EndingUIShowRequestedEvent(EndingUIData data)
    {
        Data = data;
    }
}