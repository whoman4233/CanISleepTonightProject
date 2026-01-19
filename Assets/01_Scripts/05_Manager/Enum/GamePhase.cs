
public enum GamePhase
{
    NotStarted,     // 메인 메뉴 등 게임 시작 전
    Tutorial,
    Standby,        // 오늘 점검해야 할 요주의 감방이 결정되는 단계, 날짜 카운트 +1
    Briefing,       // 1. 브리핑 페이즈 (요주의 감방 결정 및 배치)
    Patrol,         // 2. 순찰 페이즈 (플레이 시간, 제한 시간 8분)
    Settlement,     // 3. 정산 페이즈 (폭동 게이지 증감),
    OffDuty,         // 4. 퇴근 페이즈 (업무 보고를 통한 폭동 게이지 증감 확인, 폭동 게이지 100 미만 체크, 다음 날 진행)
    Ending,          // 5. 엔딩 페이즈 (엔딩 분기에 따라 진행)
    Test             // UI테스트 및 테스트환경용 페이즈
}
public enum StandbyEnterReason // 스탠바이 진입조건 UI / 다음날/게임재시작
{
    None,
    NextDay,
    RestartSameDay
}
