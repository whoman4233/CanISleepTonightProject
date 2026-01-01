
public enum GameEndingType
{
    None,
    HappyEnding1, // <Happy Ending: 계약 연장> : 7일 간의 근무를 모두 마쳤고, 최종 폭동 게이지가 30 미만인 경우
    NomalEnding1, // <Normal Ending: 상태 유지> : 7일 간의 근무를 모두 마쳤고, 최종 폭동 게이지가 30 이상 80 미만인 경우
    NomalEnding2, // <Normal Ending: 위기 회피> : 7일 간의 근무를 모두 마쳤고, 최종 폭동 게이지가 80 이상인 경우
    BadEnding1, // <Bad Ending: 단체 탈옥> : 폭동 게이지가 80 이상인 상태로 [준비 페이즈]에 진입해 [준비 페이즈] 도중 폭동 게이지가 100 이상이 되었을 때 활성화
    BadEnding2, // <Bad Ending: 산업 재해> : [순찰 페이즈] 중 HP가 0이 되었을 때 활성화
    BadEnding3, // <Bad Ending: 집단 폭동> : [정산 페이즈]의 폭동 게이지 증감 결과 폭동 게이지가 100 이상이 되었을 때 활성화
}