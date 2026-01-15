using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Mission Settings")]
    [SerializeField] private List<DailyMissionStrategy> missionScenario; // 1~7일차 SO 리스트

    private Action<ForceMissionFailRequestedEvent> _onForceMissionFailRequested; //강제종료 이벤트 

    // 현재 진행 중인 미션 (Read Only)
    public DailyMissionStrategy CurrentMission { get; private set; }

    // 미션 진행 상태
    public bool IsBriefingCompleted { get; private set; }
    public bool IsReported { get; private set; }

    // 오늘 하루 동안 플레이어가 올린 실적
    private int dailyResolvedCount = 0;

    /// <summary>
    /// 오늘 미션의 "공용 진행도"
    /// - UI(HUD), Result 판단에 사용
    /// - 미션 타입과 무관
    /// </summary>
    public int CurrentScore { get; private set; }
    private void Awake()
    {
        Instance = this;
        _onForceMissionFailRequested = OnForceMissionFailRequested;
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_onForceMissionFailRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onForceMissionFailRequested);
    }
    // 하루 시작 시 호출 (GameManager 등에서 호출)
    public void StartDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        // 인덱스 안전 검사
        if (missionScenario == null || missionScenario.Count < dayIndex)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차 미션 데이터가 없습니다!");
            return;
        }

        // 1. 오늘의 미션 갈아끼우기
        if(dayIndex < 7)
        {
            CurrentMission = missionScenario[UnityEngine.Random.Range(0, missionScenario.Count - 1)];
        }
        else
        {
            CurrentMission = missionScenario[dayIndex - 1];
        }
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 시작: {CurrentMission.title}");

        // 2. 전략 실행 (매니저 세팅)
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // 3. (필요하다면) 스포너에게 최종 소환 명령
        // PrisonerSpawnController.Instance.SpawnAll(); 

        // HUD 미션 UI 초기화 알림
        EventBus.Publish(new MissionStartedEvent
        {
            mission = CurrentMission
        });

        EventBus.Publish(new MissionProgressChangedEvent
        {
            current = CurrentScore,
            target = CurrentMission.targetScore
        });
    }

    public void StartFixDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        // 인덱스 변환 (Day 1 -> List Index 0)
        int targetIndex = dayIndex - 1;

        // 인덱스 안전 검사 (리스트 범위 체크)
        if (missionScenario == null || targetIndex < 0 || targetIndex >= missionScenario.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차에 해당하는 미션 데이터가 없습니다! (Scenario Count: {missionScenario?.Count})");
            return;
        }

        // ★ [수정] 랜덤 로직 삭제 -> 해당 날짜(인덱스)의 미션을 그대로 가져옴
        CurrentMission = missionScenario[targetIndex];

        Debug.Log($"[GameFlow] Day {dayIndex} 미션 시작: {CurrentMission.title}");

        // 2. 전략 실행 (매니저 세팅)
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // 3. (필요하다면) 스포너에게 최종 소환 명령
        // PrisonerSpawnController.Instance.SpawnAll(); 

        // HUD 미션 UI 초기화 알림
        EventBus.Publish(new MissionStartedEvent
        {
            mission = CurrentMission
        });

        EventBus.Publish(new MissionProgressChangedEvent
        {
            current = CurrentScore,
            target = CurrentMission.targetScore
        });
    }

    // ========================================================================
    // 🔥 [이벤트 훅] 외부에서 호출하는 점수 신고 전화번호
    // ========================================================================

    // 1. 아이템 찾았을 때
    public void NotifyItemFound(string itemTag)
    {
        if (CurrentMission != null)
        {
            // [수정 포인트] 미션에게 이 아이템이 목표에 맞는지 물어봅니다.
            if (CurrentMission.IsValidItem(itemTag))
            {
                // 조건에 맞을 때만 점수 증가
                CurrentScore++;

                // 미션 객체 내부 로직 실행 (필요 시)
                CurrentMission.OnEventTriggered(itemTag);

                // UI 갱신 (점수가 올랐을 때만 갱신하는 것이 효율적)
                EventBus.Publish(new MissionProgressChangedEvent
                {
                    current = CurrentScore,
                    target = CurrentMission.targetScore
                });

                Debug.Log($"[Mission] 목표 아이템 발견! 점수 증가: {CurrentScore}/{CurrentMission.targetScore}");
            }
            else
            {
                Debug.Log($"[Mission] 아이템 발견({itemTag})했으나 현재 미션 목표({CurrentMission})의 목표아이템과 다름.");
            }
        }
    }

    // 2. 죄수 제압/해결 했을 때
    public void NotifyPrisonerResolved(string cellId)
    {
        dailyResolvedCount++;
        Debug.Log($"[GameFlow] 죄수 해결 확인! (금일 누적: {dailyResolvedCount})");

        if (CurrentMission != null)
        {
            // [수정] 미션에게 "이 방(cellId) 죄수, 목표 대상 맞아?" 라고 바로 물어봅니다.
            // (미션 내부에서 ScheduleManager.Instance.GetDailyRole(cellId)를 호출하여 검사함)
            if (CurrentMission.IsValidPrisoner(cellId))
            {
                // --- 정답일 때 처리 ---

                // 1. 미션 내부 이벤트 트리거 (필요 시)
                CurrentMission.OnEventTriggered("PrisonerResolved");

                // 2. 점수 증가
                CurrentScore++;

                // 3. UI 갱신 알림
                EventBus.Publish(new MissionProgressChangedEvent
                {
                    current = CurrentScore,
                    target = CurrentMission.targetScore
                });

                Debug.Log($"[Mission] 타겟 죄수 제압 성공! 점수 증가.");
            }
            else
            {
                // --- 오답(일반 죄수)일 때 처리 ---
                Debug.Log($"[Mission] 죄수 제압({cellId})했으나 타겟 조건에 부합하지 않음.");
            }
        }
    }

    // 3. 하루 결산 (SettlementTrigger에서 호출)
    public bool EvaluateDayResult(out string failReason)
    {
        if (CurrentMission == null)
        {
            failReason = "미션 정보 없음";
            return true;
        }

        // 미션 SO에게 "나 오늘 이만큼(dailyResolvedCount) 했어, 합격이야?" 물어봄
        return CurrentMission.CheckWinCondition(CurrentScore, out failReason);
    }

    // [추가] 외부(테스트 콘솔 등)에서 특정 날짜의 미션 데이터를 요청할 때 사용
    public DailyMissionStrategy GetMissionStrategy(int dayIndex)
    {
        // 1일차 = index 0 이므로 -1 처리
        int listIndex = dayIndex - 1;

        if (missionScenario != null && listIndex >= 0 && listIndex < missionScenario.Count)
        {
            return missionScenario[listIndex];
        }

        Debug.LogWarning($"[DailyMissionManager] {dayIndex}일차 미션 데이터가 없습니다.");
        return null;
    }

    // 브리핑/결과 보고 추적용 bool 값 메서드
    public void MarkBriefingCompleted()
    {
        IsBriefingCompleted = true;
    }

    public void MarkReported()
    {
        IsReported = true;
    }

    public void ResetDailyFlags()
    {
        IsBriefingCompleted = false;
        IsReported = false;
    }
    private void OnForceMissionFailRequested(ForceMissionFailRequestedEvent e) //미션 강제 실패 
    {
        bool success = false;
        string failReason;

        EvaluateDayResult(out failReason);

        EventBus.Publish(
            new ResultUIShowRequestedEvent(success, failReason)
        );
    }
}
