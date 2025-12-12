
public enum GamePhase
{
    NotStarted,     // 메인 메뉴 등 게임 시작 전
    Briefing,       // 1. 브리핑 페이즈 (요주의 감방 결정 및 배치)
    Patrol,         // 2. 순찰 페이즈 (플레이 시간, 제한 시간 8분)
    Settlement,     // 3. 정산 페이즈 (업무 보고, 폭동 게이지 증감)
    Result,         // 4. 결과 페이즈 (폭동 게이지 100 미만 체크, 다음 날 진행)
    GameOver,       // 폭동 게이지 100 도달 또는 기타 실패
    Ending          // 7일차 생존 성공
}

