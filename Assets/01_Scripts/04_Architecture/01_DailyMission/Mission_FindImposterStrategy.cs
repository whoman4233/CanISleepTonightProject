using UnityEngine;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Mission 4에서만 사용하는 다이얼로그 트리거 정의 데이터
/// - SO(Mission_FindImposterStrategy)에 포함됨
/// - 실제 Instantiate는 MonoBehaviour가 담당
/// </summary>
[System.Serializable]
public class Mission4DialogueTrigger
{
    [Tooltip("미션 내에서 유니크한 트리거 ID")]
    public string triggerId;

    [Tooltip("재생할 다이얼로그 SO")]
    public TriggerDialogueSO dialogue;

    [Tooltip("DialogueTrigger가 붙어있는 프리팹")]
    public GameObject triggerPrefab;

    [Tooltip("월드 좌표 기준 생성 위치")]
    public Vector3 spawnPosition;

    [Tooltip("월드 좌표 기준 회전값")]
    public Vector3 spawnRotation;
}

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

    // =========================
    // Mission 4 Dialogue Triggers
    // =========================

    [Header("Mission 4 Dialogue Triggers")]
    [SerializeField] private List<Mission4DialogueTrigger> dialogueTriggers;
    
    [Header("Sequence Options")]
    [SerializeField] private SequenceOptionSO failSequence;
    [SerializeField] private SequenceOptionSO successSequence;
    public SequenceOptionSO FailSequence => failSequence;
    public SequenceOptionSO SuccessSequence => successSequence;

    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        base.SetupDay(ad, sm);

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

        // ===== Mission 4 전용 트리거 소환 요청 =====
        EventBus.Publish(new Mission4DialogueTriggerSpawnEvent
        {
            Triggers = dialogueTriggers
        });
    }

    // =========================
    // Dialogue Trigger Control
    // =========================
    public bool CanUseTrigger(string triggerId)
    {
        // SO 내부 HashSet 사용 금지
        return !DailyMissionManager.Instance
            .CurrentMissionRuntime
            .usedDialogueTriggerIds
            .Contains(triggerId);
    }

    public void MarkTriggerUsed(string triggerId)
    {
        // 런타임 컨테이너에만 기록
        DailyMissionManager.Instance
            .CurrentMissionRuntime
            .usedDialogueTriggerIds
            .Add(triggerId);
    }


    // 플레이어가 죄수를 제압했을 때 DailyMissionManager가 호출하는 함수
    public override bool IsValidPrisoner(string cellId)
    {
        var role = PrisonerScheduleManager.Instance.GetDailyRole(cellId);
        var runtime = DailyMissionManager.Instance.CurrentMissionRuntime;

        // 1. 진짜 프랭크 → 즉시 실패
        if (role.visualType == realFrankType)
        {
            runtime.killedRealFrank = true;
            runtime.immediateFailTriggered = true;
            if (failSequence != null)
            {
                EventBus.Publish(new SequencePlayRequestedEvent
                {
                    Sequence = failSequence,
                    TargetPoint = null
                });
            }
            return false;
        }

        // 2. 가짜 프랭크
        if (fakeFrankTypes.Contains(role.visualType))
        {
            // 점수는 DailyMissionManager에서 증가됨
            int nextScore = DailyMissionManager.Instance.CurrentScore + 1;

            if (nextScore >= targetScore && successSequence != null)
            {
                EventBus.Publish(new SequencePlayRequestedEvent
                {
                    Sequence = successSequence
                });
            }
    
            return true;
        }
        return false;
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        var runtime = DailyMissionManager.Instance.CurrentMissionRuntime;

        // SO 내부 상태 참조 금지
        if (runtime.killedRealFrank)
        {
            failReason = failReasonText;
            return false;
        }

        if (currentScore < targetScore)
        {
            failReason = $"가짜를 모두 찾지 못했습니다. ({currentScore}/{targetScore})";
            return false;
        }

        failReason = "";
        return true;
    }

}