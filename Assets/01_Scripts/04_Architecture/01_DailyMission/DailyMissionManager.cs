using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Mission Settings")]
    [SerializeField] private List<DailyMissionStrategy> missionScenario; // 1~7일차 SO 리스트

    private Action<ForceMissionFailRequestedEvent> _onForceMissionFailRequested;

    // 현재 진행 중인 미션 (Read Only)
    public DailyMissionStrategy CurrentMission { get; private set; }

    public bool IsBriefingCompleted { get; private set; }
    public bool IsReported { get; private set; }

    private int dailyResolvedCount = 0;
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

        if (missionScenario == null || missionScenario.Count < dayIndex)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차 미션 데이터가 없습니다!");
            return;
        }

        // 1. 오늘의 미션 갈아끼우기
        if (dayIndex < 7)
        {
            CurrentMission = missionScenario[UnityEngine.Random.Range(0, missionScenario.Count - 1)];
        }
        else
        {
            CurrentMission = missionScenario[dayIndex - 1];
        }
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 시작: {CurrentMission.title}");

        // 2. 전략 실행 (테마 설정 및 역할 배정)
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // ★ [추가] 3. 이상현상 배정 실행 (이게 없어서 안 나왔던 것!)
        // SetupDay에서 역할(Role) 배정이 끝난 뒤에 호출해야 정확하게 배정됨
        if (AnomalyDistributor.Instance != null)
        {
            AnomalyDistributor.Instance.DistributeAnomalies();
        }

        // 4. (필요하다면) 스포너에게 최종 소환 명령
        // PrisonerSpawnController.Instance.SpawnAll(); 

        EventBus.Publish(new MissionStartedEvent { mission = CurrentMission });
        EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });
    }

    public void StartFixDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        int targetIndex = dayIndex - 1;

        if (missionScenario == null || targetIndex < 0 || targetIndex >= missionScenario.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차에 해당하는 미션 데이터가 없습니다! (Scenario Count: {missionScenario?.Count})");
            return;
        }

        CurrentMission = missionScenario[targetIndex];

        Debug.Log($"[GameFlow] Day {dayIndex} 미션 시작: {CurrentMission.title}");

        // 2. 전략 실행 (테마 설정 및 역할 배정)
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // ★ [추가] 3. 이상현상 배정 실행
        if (AnomalyDistributor.Instance != null)
        {
            AnomalyDistributor.Instance.DistributeAnomalies();
        }

        EventBus.Publish(new MissionStartedEvent { mission = CurrentMission });
        EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });
    }

    // ========================================================================
    // 🔥 [이벤트 훅]
    // ========================================================================

    public void NotifyItemFound(string itemTag)
    {
        if (CurrentMission != null)
        {
            if (CurrentMission.IsValidItem(itemTag))
            {
                CurrentScore++;
                CurrentMission.OnEventTriggered(itemTag);

                EventBus.Publish(new MissionProgressChangedEvent
                {
                    current = CurrentScore,
                    target = CurrentMission.targetScore
                });

                Debug.Log($"[Mission] 목표 아이템 발견! 점수 증가: {CurrentScore}/{CurrentMission.targetScore}");
            }
            else
            {
                Debug.Log($"[Mission] 아이템 발견({itemTag})했으나 목표 아님.");
            }
        }
    }

    public void NotifyPrisonerResolved(string cellId)
    {
        dailyResolvedCount++;
        Debug.Log($"[GameFlow] 죄수 해결 확인! (금일 누적: {dailyResolvedCount})");

        if (CurrentMission != null)
        {
            if (CurrentMission.IsValidPrisoner(cellId))
            {
                CurrentMission.OnEventTriggered("PrisonerResolved");
                CurrentScore++;

                EventBus.Publish(new MissionProgressChangedEvent
                {
                    current = CurrentScore,
                    target = CurrentMission.targetScore
                });

                Debug.Log($"[Mission] 타겟 죄수 제압 성공! 점수 증가.");
            }
            else
            {
                Debug.Log($"[Mission] 죄수 제압({cellId})했으나 타겟 아님.");
            }
        }
    }

    public bool EvaluateDayResult(out string failReason)
    {
        if (CurrentMission == null)
        {
            failReason = "미션 정보 없음";
            return true;
        }
        return CurrentMission.CheckWinCondition(CurrentScore, out failReason);
    }

    public DailyMissionStrategy GetMissionStrategy(int dayIndex)
    {
        int listIndex = dayIndex - 1;
        if (missionScenario != null && listIndex >= 0 && listIndex < missionScenario.Count)
        {
            return missionScenario[listIndex];
        }
        Debug.LogWarning($"[DailyMissionManager] {dayIndex}일차 미션 데이터가 없습니다.");
        return null;
    }

    public void MarkBriefingCompleted() => IsBriefingCompleted = true;
    public void MarkReported() => IsReported = true;
    public void ResetDailyFlags()
    {
        IsBriefingCompleted = false;
        IsReported = false;
    }

    private void OnForceMissionFailRequested(ForceMissionFailRequestedEvent e)
    {
        bool success = false;
        string failReason;
        EvaluateDayResult(out failReason);
        EventBus.Publish(new ResultUIShowRequestedEvent(success, failReason));
    }
}