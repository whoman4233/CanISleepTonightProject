
public enum GameEndingType
{
    None,
    HappyEnding1, // 상태 유지(7일 무사히 마침)
    BadEnding1, // 순직 처리
    BadEnding2, // 산업 재해(7일 이전에 폭동 100 이상)
    BadEnding3, // 위기 회피(7일차에 폭동 100 이상으로 퇴근)
}