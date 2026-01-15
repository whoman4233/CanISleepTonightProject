using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Missions/Type: Find Imposter (Day 4)")]
public class Mission_FindImposterStrategy : DailyMissionStrategy
{
    [Header("Imposter Settings")]
    [Tooltip("진짜 프랭크의 외형 타입 (잡으면 실패)")]
    public VisualAnomalyType realFrankType;

    [Tooltip("가짜 프랭크들의 외형 타입 (잡아야 성공)")]
    public List<VisualAnomalyType> fakeFrankTypes;

    [Tooltip("진짜를 잡았을 때 실패 사유 텍스트")]
    public string failReasonText = "진짜 프랭크를 공격하여 제압 실패";

    // 런타임 상태 변수
    private bool _killedRealFrank = false;

    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        base.SetupDay(ad, sm);
        _killedRealFrank = false; // 상태 초기화

        // 1. 모든 방 초기화 (범인 0명)
        sm.AssignRolesForNewDay(suspiciousCount: 0, defaultAI: PrisonerAIType.Good);

        // 2. 방 리스트 섞기
        var shuffledCells = sm.GetActiveCellIds().OrderBy(x => Random.value).ToList();
        int cellIndex = 0;

        // 3. 진짜 프랭크 배정 (1명)
        // Suspicious = false (점호 대상 아님, 하지만 잡으면 안됨)
        if (cellIndex < shuffledCells.Count)
        {
            sm.SetDailyRole(shuffledCells[cellIndex], PrisonerAIType.Good, realFrankType, false);
            cellIndex++;
        }

        // 4. 가짜 프랭크 배정 (리스트 개수만큼)
        // Suspicious = true (범인 판정)
        foreach (var fakeType in fakeFrankTypes)
        {
            if (cellIndex >= shuffledCells.Count) break;

            // 가짜는 나쁜 AI? 혹은 흉내내는 AI? 기획에 맞게 설정 (여기선 Bad로 설정)
            sm.SetDailyRole(shuffledCells[cellIndex], PrisonerAIType.Bad, fakeType, true);
            cellIndex++;
        }

        Debug.Log($"[ImposterMission] 배정 완료. Real: {realFrankType}, Fakes: {fakeFrankTypes.Count}");
    }

    // 플레이어가 죄수를 제압했을 때 DailyMissionManager가 호출하는 함수
    public override bool IsValidPrisoner(string cellId)
    {
        var role = PrisonerScheduleManager.Instance.GetDailyRole(cellId);

        // 1. 진짜 프랭크를 잡았을 경우 -> 즉시 실패 처리
        if (role.visualType == realFrankType)
        {
            Debug.Log("<color=red>[Mission Failed] 진짜 프랭크를 제압했습니다!</color>");
            HandleRealFrankSuppressed();
            return false; // 점수 안 오름
        }

        // 2. 가짜 프랭크를 잡았을 경우 -> 점수 인정
        if (fakeFrankTypes.Contains(role.visualType))
        {
            return true;
        }

        return false;
    }

    private void HandleRealFrankSuppressed()
    {
        _killedRealFrank = true;

        // 1. 대사 출력 (추후 구현 예정이라 하셨지만, 일단 로그나 팝업 이벤트 발생)
        // "너 지금 누굴 때리는 거야 멍청아!"
        Debug.Log("[Dialogue] Frank: '너 지금 누굴 때리는 거야 멍청아!'");
        EventBus.Publish(new ShowTimedTextPopupEvent("진짜 프랭크를 공격했습니다!", 2.0f));

        // 2. 강제 정산 페이즈 진입 (실패)
        // 약간의 딜레이를 두고 넘기는 게 자연스러우므로 코루틴 등을 쓰면 좋겠지만,
        // 여기선 즉시 요청하거나 GameManager를 통해 처리
        EventBus.Publish(new ForceMissionFailRequestedEvent());

        //=======================================
        //미션 강제 실패 시 아예 게임이 멈추는 현상이 있어서 하단의 게임매니저 페이즈 변경 주석처리했습니다.
        //=======================================

        //if (GameManager.Instance != null)
        //{
        //    // 대사 읽을 시간 2초 정도 뒤에 넘어가도록 코루틴 호출 권장 (GameManager 위임)
        //    // 여기서는 즉시 전환 예시:
        //    GameManager.Instance.ChangePhase(GamePhase.Settlement);
        //}
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        // 1. 진짜를 잡아서 강제 종료된 경우
        if (_killedRealFrank)
        {
            failReason = failReasonText;
            return false;
        }

        // 2. 가짜를 목표치만큼 못 잡은 경우
        if (currentScore < targetScore)
        {
            failReason = $"가짜를 모두 찾지 못했습니다. ({currentScore}/{targetScore})";
            return false;
        }

        failReason = "";
        return true;
    }
}