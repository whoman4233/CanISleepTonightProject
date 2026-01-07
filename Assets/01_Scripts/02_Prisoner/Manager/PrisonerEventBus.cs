using System;

/// <summary>
/// 죄수 / 진압 전용 로컬 이벤트 버스
/// - 진압 세션 내부 통신용
/// - 전역 게임 흐름에는 관여하지 않음
/// </summary>
public static class PrisonerEventBus
{
    // 진압 시작 (Inspection → 전투 파트)
    public static event Action<string> OnSuppressSessionStarted;
    public static void RaiseSuppressSessionStarted(string cellId)
        => OnSuppressSessionStarted?.Invoke(cellId);

    // 죄수 피격
    public static event Action<string, int> OnPrisonerHit;
    public static void RaisePrisonerHit(string prisonerId, int damage)
        => OnPrisonerHit?.Invoke(prisonerId, damage);

    // 죄수 사망(Down)
    public static event Action<string> OnPrisonerDown;
    public static void RaisePrisonerDown(string prisonerId)
        => OnPrisonerDown?.Invoke(prisonerId);

    // 해당 감방의 죄수 전원 Down
    public static event Action<string> OnAllPrisonersDown;
    public static void RaiseAllPrisonersDown(string cellId)
        => OnAllPrisonersDown?.Invoke(cellId);

    public static event Action<string> OnForceOpenDoor;
    public static void PublishForceOpenDoor(string cellId)
        => OnForceOpenDoor?.Invoke(cellId);

}
