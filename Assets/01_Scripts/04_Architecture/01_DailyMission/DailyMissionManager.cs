using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Mission Settings")]
    [SerializeField] private List<DailyMissionStrategy> missionScenario;

    // =====================================================
    // 런 전체에서 고정되는 미션 테이블 (새 게임 / 실패 후 새 게임에서만 생성)
    // Save / Load 대상
    // =====================================================
    private List<DailyMissionStrategy> _randomizedMissionOrder = new List<DailyMissionStrategy>();

    // =====================================================
    // 다음 날마다 줄어드는 풀
    // 하루 성공 시 미션 하나씩 제거됨
    // Save / Load 대상
    // =====================================================
    private List<DailyMissionStrategy> _remainingMissionOrder = new List<DailyMissionStrategy>();

    private Action<MissionEndRequestedEvent> _onMissionEndRequested;
    private Action<GameContextReadyEvent> _onGameContextReady;

    public DailyMissionStrategy CurrentMission { get; private set; }
    public MissionRuntimeState CurrentMissionRuntime { get; private set; } // 현재 미션 런타임상태(미션04)

    public bool IsBriefingCompleted { get; private set; }
    public bool IsBriefingDialogueViewed { get; private set; }
    public bool IsReported { get; private set; }

    private int dailyResolvedCount = 0; 
    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _onMissionEndRequested = OnMissionEndRequested;
        _onGameContextReady = OnGameContextReady;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onMissionEndRequested);
        EventBus.Subscribe(_onGameContextReady);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onMissionEndRequested);
        EventBus.Unsubscribe(_onGameContextReady);
    }
    // =====================================================
    // 런 전체 기준으로 미션 테이블이 유효한가
    // (Day 전환 / 씬 재로딩 / 다음날 진입 판단용)
    // =====================================================
    public bool HasValidRunMissionTable
    {
        get => _randomizedMissionOrder != null && _randomizedMissionOrder.Count > 0;
    }
    public bool HasRemainingMission
    {
        get => _remainingMissionOrder != null && _remainingMissionOrder.Count > 0;
    }
    public int GetMissionIndex(DailyMissionStrategy mission) //미션 번호 조회
    {
        return missionScenario.IndexOf(mission);
    }
    private void OnGameContextReady(GameContextReadyEvent e)
    {
        Debug.Log($"[DailyMissionManager] GameContextReady (Day {e.CurrentDay})");

        // =====================================================
        // Day 단위 상태만 리셋
        // (미션 테이블 / 런타임 상태는 건드리지 않음)
        // =====================================================
        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;
    }

    // =====================================================
    // 새 게임 전용 미션 테이블 생성 API
    // - 새 게임
    // - 실패 재시작
    // - 튜토리얼 스킵 시작
    // =====================================================
    public void CreateNewMissionTableForNewRun()
    {
        Debug.Log("[Mission] 새 런 시작 → 미션 테이블 재생성");

        _randomizedMissionOrder.Clear();
        _remainingMissionOrder.Clear();

        CurrentMission = null;
        CurrentMissionRuntime = null;

        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;

        InitializeMissionTableForRun();
    }

    // =====================================================
    // 런 단위 미션 테이블 생성
    // - 1~6 랜덤 + 7 고정
    // =====================================================
    public void InitializeMissionTableForRun()
    {
        if (missionScenario == null || missionScenario.Count < 7)
        {
            Debug.LogError("[Mission] 미션 시나리오 개수가 부족합니다.");
            return;
        }

        var normalDays = missionScenario.GetRange(0, 6);
        ShuffleList(normalDays);

        _randomizedMissionOrder.AddRange(normalDays);
        _randomizedMissionOrder.Add(missionScenario[6]); // Day 7 고정

        // 남은 미션 테이블은 복사본
        _remainingMissionOrder = new List<DailyMissionStrategy>(_randomizedMissionOrder);

        Debug.Log("[Mission] 런 미션 테이블 생성 완료");
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void StartDay(int dayIndex)
    {
        // 이어하기 / 중단 복귀 시 처리
        if (CurrentMission != null)
        {
            Debug.Log($"[Mission] Day {dayIndex} 기존 미션 유지: {CurrentMission.title}");
            StartMissionSetup(dayIndex);
            return;
        }

        // ===============================
        // "신규 Day" 상태 초기화
        // ===============================
        dailyResolvedCount = 0;
        CurrentScore = 0;

        // [방어 로직] 남은 미션이 아예 없는 경우 런 테이블에서 복구 시도
        if (_remainingMissionOrder.Count == 0)
        {
            Debug.LogWarning("[Mission] 남은 미션이 없습니다! 런 테이블에서 복구를 시도합니다.");
            RestoreRemainingFromRunTable();

            if (_remainingMissionOrder.Count == 0)
            {
                Debug.LogError("[Mission] 복구 실패: 진행할 수 있는 미션이 없습니다.");
                return;
            }
        }

        // =================================================
        // 미션 선택 로직 (7일차 고정 vs 1~6일차 랜덤)
        // =================================================

        if (dayIndex >= 6)
        {
            CurrentMission = missionScenario[6]; // Day 7 고정 미션 (리스트의 7번째 요소)
            Debug.Log("[Mission] 7일차 고정 미션(BossRiot) 진입");
        }
        else
        {
            // Day 1~6: 7번 미션을 제외한 남은 미션 중 랜덤 선택
            var candidates = _remainingMissionOrder
                .Where(m => m != missionScenario[6])
                .ToList();

            if (candidates.Count == 0)
            {
                // [수정 포인트] 1~6일차 미션이 다 떨어졌는데 날짜가 아직 7이 아닐 경우
                // 에러를 내는 대신 7일차 미션으로 조기 진입시킵니다.
                Debug.LogWarning($"[Mission] Day {dayIndex}이지만 랜덤 미션이 소진됨. 7일차 미션을 앞당겨 시작합니다.");
                CurrentMission = missionScenario[6];
            }
            else
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                CurrentMission = candidates[index];
            }
        }

        CurrentMissionRuntime = new MissionRuntimeState();
        StartMissionSetup(dayIndex);
    }


    // =====================================================
    // 하루 성공 확정 시 호출
    // - ResultUIConfirmedEvent 이후
    // =====================================================
    public void ConsumeCurrentMission()
    {
        if (CurrentMission == null)
            return;

        _remainingMissionOrder.Remove(CurrentMission);
        CurrentMission = null;         
        CurrentMissionRuntime = null;

        Debug.Log("[Mission] 오늘 미션 소비 완료");
    }

    // =====================================================
    // 디버그용: 오늘 날짜에 해당하는 미션 강제 배정 (수정됨)
    // =====================================================
    public void StartFixDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        // 혹시 리스트가 비어있으면 채워줌
        if (_randomizedMissionOrder.Count == 0)
        {
            Debug.LogWarning("[Debug] 런 미션 테이블 비어있음 → 재생성");
            InitializeMissionTableForRun();
        }

        // =========================================================
        // [수정 전] 랜덤 목록에서 또 랜덤으로 뽑음 (문제 원인)
        // var candidates = ... 
        // var fixedMission = candidates[UnityEngine.Random.Range(...)];
        // =========================================================

        // =========================================================
        // [수정 후] 시나리오 원본 리스트에서 정확히 해당 인덱스 미션을 가져옴
        // =========================================================
        int targetIndex = dayIndex - 1; // 1번 키 누르면 index 0 (첫 번째 미션)

        if (missionScenario != null && targetIndex >= 0 && targetIndex < missionScenario.Count)
        {
            var fixedMission = missionScenario[targetIndex];

            CurrentMission = fixedMission;
            CurrentMissionRuntime = new MissionRuntimeState();

            // 중복 방지용 최소 동기화 (진행 중인 미션 목록에서 제거)
            if (_remainingMissionOrder.Contains(fixedMission))
            {
                _remainingMissionOrder.Remove(fixedMission);
            }

            Debug.Log($"[GameFlow] (Debug) Day {dayIndex} 고정 미션 강제 시작: {CurrentMission.title}");
            StartMissionSetup(dayIndex);
        }
        else
        {
            Debug.LogError($"[Debug] 잘못된 미션 번호입니다: {dayIndex}. (MissionScenario 범위 확인 필요)");
        }
    }

    // ========================================================================
    // 실행 순서: SetupDay(역할배정) -> Distribute(아이템배포)
    // ========================================================================
    private void StartMissionSetup(int dayIndex)
    {
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 설정 중...");

        if (CurrentMissionRuntime == null)
        {
            CurrentMissionRuntime = new MissionRuntimeState();
        }

        if (CurrentMission != null)
        {
            CurrentMission.SetupDay(
                AnomalyDistributor.Instance,
                PrisonerScheduleManager.Instance
            );
        }

        if (AnomalyDistributor.Instance != null)
        {
            AnomalyDistributor.Instance.DistributeAnomalies();
        }

        EventBus.Publish(new MissionStartedEvent { mission = CurrentMission });
        EventBus.Publish(new MissionProgressChangedEvent
        {
            current = CurrentScore,
            target = CurrentMission.targetScore
        });
        GameManager.Instance?.SaveNow();
    }
    // =====================================================
    // 런 유지 상태에서 남은 미션이 비었을 경우 복구용
    // (씬 재로딩 보호 장치)
    // =====================================================
    public void RestoreRemainingFromRunTable()
    {
        if (_remainingMissionOrder.Count > 0)
            return;

        if (_randomizedMissionOrder.Count == 0)
        {
            Debug.LogError("[Mission] 런 테이블도 비어 있음 → 복구 불가");
            return;
        }

        _remainingMissionOrder = new List<DailyMissionStrategy>(_randomizedMissionOrder);
        Debug.Log("[Mission] 씬 재로딩으로 인해 남은 미션 테이블 복구됨");
    }
    public void NotifyItemFound(string itemTag)
    {
        if (CurrentMission != null && CurrentMission.IsValidItem(itemTag))
        {
            CurrentScore++;
            CurrentMission.OnEventTriggered(itemTag);
            EventBus.Publish(new MissionProgressChangedEvent
            {
                current = CurrentScore,
                target = CurrentMission.targetScore
            });
        }
    }

    public void NotifyPrisonerResolved(string cellId)
    {
        dailyResolvedCount++;
        Debug.Log($"[GameFlow] 죄수 제압됨: {cellId} (누적: {dailyResolvedCount})");

        if (CurrentMission != null && CurrentMission.IsValidPrisoner(cellId))
        {
            CurrentScore++;

            // [디버그 추가] 현재 점수와 목표 점수 확인
            Debug.Log($"<color=cyan>[Check] 점수 증가! 현재: {CurrentScore} / 목표: {CurrentMission.targetScore}</color>");

            CurrentMission.OnEventTriggered("PrisonerResolved");
            EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });

            // [핵심] 여기서 이벤트가 발행되는지 확인
            if (CurrentScore >= CurrentMission.targetScore)
            {
                Debug.Log($"<color=green>[Success] 목표 달성! 성공 이벤트 발행함.</color>");
                EventBus.Publish(new MissionEndRequestedEvent(true));
            }
        }
        else
        {
            // 타겟이 아니거나 미션이 없을 때
            Debug.Log($"[Check] 타겟 아님. (CurrentMission: {CurrentMission?.name}, IsValid: {CurrentMission?.IsValidPrisoner(cellId)})");
        }
    }

    public bool EvaluateDayResult(out string failReason)
    {
        if (CurrentMission == null)
        {
            failReason = "미션 정보 없음";
            return false;
        }

        return CurrentMission.CheckWinCondition(CurrentScore, out failReason);
    }

    public DailyMissionStrategy GetMissionStrategy(int dayIndex)
    {
        if (_randomizedMissionOrder.Count == 0)
            InitializeMissionTableForRun();

        int index = dayIndex - 1;
        if (index >= 0 && index < _randomizedMissionOrder.Count)
            return _randomizedMissionOrder[index];

        return null;
    }

    public void ResetDailyFlags()
    {
        IsBriefingCompleted = false;
        IsReported = false;
    }

    public List<int> GetMissionOrderIndices()
    {
        return _randomizedMissionOrder
            .Select(m => missionScenario.IndexOf(m))
            .ToList();
    }

    public void RestoreMissionOrder(List<int> savedIndices)
    {
        _randomizedMissionOrder.Clear();
        _remainingMissionOrder.Clear();

        foreach (int index in savedIndices)
        {
            if (index >= 0 && index < missionScenario.Count)
            {
                _randomizedMissionOrder.Add(missionScenario[index]);
            }
        }

        _remainingMissionOrder = new List<DailyMissionStrategy>(_randomizedMissionOrder);
    }
    public void RestoreCurrentMission(int missionIndex)
    {
        if (missionIndex < 0 || missionIndex >= missionScenario.Count)
        {
            Debug.LogWarning("[Mission] 잘못된 미션 인덱스 → 복원 실패");
            return;
        }

        CurrentMission = missionScenario[missionIndex];
        CurrentMissionRuntime = new MissionRuntimeState();

        Debug.Log($"[Mission] 이어하기 → 미션 복원: {CurrentMission.title}");
    }

    private void OnMissionEndRequested(MissionEndRequestedEvent e)
    {
        Debug.Log($"[Debug] 1. 미션 종료 요청 수신됨 (성공여부: {e.IsSuccess})");

        if (CurrentMission is Mission_FindImposterStrategy imposter)
        {
            Debug.Log("[Debug] 2. 현재 미션은 'Mission 4(프랭크 찾기)' 입니다.");

            // 시퀀스 데이터가 있는지 확인
            bool hasSequence = (imposter.SuccessSequence != null);
            Debug.Log($"[Debug] 3. 시퀀스 존재 여부: {hasSequence}");

            if (e.IsSuccess && hasSequence)
            {
                Debug.Log("[Debug] 4. 조건 만족: 성공했고 + 시퀀스가 있음 -> 시퀀스 재생 요청");

                EventBus.Publish(new SequencePlayRequestedEvent
                {
                    Sequence = imposter.SuccessSequence
                });

                // 의심 구간 
                Debug.Log("<color=red>[Debug] 5. 여기서 return이 실행되어 함수가 종료됩니다! (UI 안 뜸)</color>");
                return;
            }
            else
            {
                Debug.Log("[Debug] 4-B. 조건 불만족: 실패했거나 시퀀스가 없음 -> 다음 단계로 진행");
            }
        }
        else
        {
            Debug.Log($"[Debug] 2-B. 현재 미션은 프랭크 찾기가 아닙니다. ({CurrentMission?.GetType().Name})");
        }
    }

    public void ResetAll()
    {
        Debug.Log("[DailyMissionManager] ResetAll (New Game)");

        CurrentMission = null;
        CurrentMissionRuntime = null;

        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;

        InitializeMissionTableForRun();
    }
    public void ResetToTitle() // 인트로씬 왔다갔다하면서 중복미션 방지하기 위한 코드
    {
        Debug.Log("[Mission] 타이틀 복귀 및 모든 미션 데이터 파기");

        // 리스트 자체를 새로 할당하여 이전 참조를 완전히 끊음
        _randomizedMissionOrder = new List<DailyMissionStrategy>();
        _remainingMissionOrder = new List<DailyMissionStrategy>();

        CurrentMission = null;
        CurrentMissionRuntime = null;

        // 플래그 초기화
        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;
        dailyResolvedCount = 0;
        CurrentScore = 0;
    }

    public void MarkBriefingCompleted() => IsBriefingCompleted = true;
    public void MarkBriefingDialogueViewed() => IsBriefingDialogueViewed = true;
    public void MarkReported() => IsReported = true;
}
