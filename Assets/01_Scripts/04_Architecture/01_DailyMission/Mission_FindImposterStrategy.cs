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

    [Header("Sequence Options")]
    [SerializeField] private SequenceOptionSO failSequence;
    [SerializeField] private SequenceOptionSO successSequence;

    [Header("Special Dialogue Keys")]
    [SerializeField] private string immediateFailDialogueKey;   // DTxt_KR_M04_09 실패 다이얼로그
    [SerializeField] private string immediateSuccessDialogueKey; // DTxt_KR_M04_08 성공 다이얼로그
    public string ImmediateFailDialogueKey => immediateFailDialogueKey;
    public string ImmediateSuccessDialogueKey => immediateSuccessDialogueKey;
    // 런타임 상태 변수
    private bool _killedRealFrank = false;

    //실패 분기 처리용 bool 값
    private bool _immediateFailTriggered;
    public bool ImmediateFailTriggered => _immediateFailTriggered;
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

        _immediateFailTriggered = true;

        // =====================================================
        // 연출 시퀀스 요청
        // =====================================================
        if (failSequence != null)
        {
            EventBus.Publish(new SequencePlayRequestedEvent
            {
                Sequence = failSequence,
                TargetPoint = null // NPC ArrivalPoint는 SequenceExecutor 쪽에서 지정
            });
        }
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