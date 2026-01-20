using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Mission Settings")]
    [SerializeField] private List<DailyMissionStrategy> missionScenario; // 전체 미션 풀 (1~6일차 + 7일차)

    // 게임 시작 시 랜덤하게 섞인 1~6일차 미션 목록을 저장할 리스트
    private List<DailyMissionStrategy> _randomizedMissionOrder = new List<DailyMissionStrategy>();

    private Action<MissionEndRequestedEvent> _onMissionEndRequested;
    private Action<GameContextReadyEvent> _onGameContextReady;
    public DailyMissionStrategy CurrentMission { get; private set; }

    // =========================
    // Day 단위 플래그들
    // =========================
    public bool IsBriefingCompleted { get; private set; }
    public bool IsBriefingDialogueViewed { get; private set; } //  NPC 대화 강제 가이드용
    public bool IsReported { get; private set; }

    private int dailyResolvedCount = 0;
    public int CurrentScore { get; private set; }

    private void Awake()
    {
        Instance = this;
        _onMissionEndRequested = OnMissionEndRequested;
        _onGameContextReady = OnGameContextReady;

        // 게임 시작 시 미션 순서 미리 섞기
        // (세이브 파일 로드 시에는 이 순서가 덮어씌워져야 함)
        InitializeMissionOrder();
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

    // =========================================================
    // GameContextReadyEvent 수신
    // - Day 시작 / 이어하기 / 새로하기 / 씬 재진입 공통 진입점
    // =========================================================
    private void OnGameContextReady(GameContextReadyEvent e)
    {
        Debug.Log($"[DailyMissionManager] GameContextReady (Day {e.CurrentDay})");

        // Day 단위 상태 전부 리셋
        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;
    }

    // ★ 1~6일차 미션을 섞어서 리스트에 저장
    public void InitializeMissionOrder()
    {
        _randomizedMissionOrder.Clear();

        if (missionScenario == null || missionScenario.Count == 0) return;

        // 방어 코드: 미션이 7개 미만이면 있는대로 다 섞어서 넣음
        if (missionScenario.Count < 7)
        {
            Debug.LogError("[Mission] 미션 시나리오 개수가 7개 미만입니다! (7일차 고정 불가)");
            _randomizedMissionOrder.AddRange(missionScenario);
            ShuffleList(_randomizedMissionOrder);
            return;
        }

        // 1. 1~6일차 (인덱스 0~5) 추출 후 섞기
        var normalDays = missionScenario.GetRange(0, 6);
        ShuffleList(normalDays);

        // 2. 섞인 1~6일차 추가
        _randomizedMissionOrder.AddRange(normalDays);

        // 3. 7일차 (인덱스 6) 고정 추가
        _randomizedMissionOrder.Add(missionScenario[6]);

        Debug.Log("[Mission] 미션 순서 재설정 완료: [Day 1~6 Random] + [Day 7 Fixed]");
    }

    // 피셔-예이츠 셔플
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

    // [정상 플레이] 하루 시작 시 호출
    public void StartDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        if (_randomizedMissionOrder.Count == 0) InitializeMissionOrder();

        // 인덱스 조정 (dayIndex는 1부터 시작하므로 -1)
        int listIndex = dayIndex - 1;

        if (listIndex < 0 || listIndex >= _randomizedMissionOrder.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차 미션을 찾을 수 없습니다! (범위 초과)");
            return;
        }

        // 미리 섞어둔 리스트에서 꺼내옴
        CurrentMission = _randomizedMissionOrder[listIndex];

        StartMissionSetup(dayIndex);
    }

    // [테스트용] 특정 미션 강제 실행 (FixDay)
    public void StartFixDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        int targetIndex = dayIndex - 1; // 1-based -> 0-based

        if (missionScenario == null || targetIndex < 0 || targetIndex >= missionScenario.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차에 해당하는 원본 미션 데이터가 없습니다!");
            return;
        }

        // 1. 원본 리스트에서 고정 미션을 가져옵니다.
        var fixedMission = missionScenario[targetIndex];
        CurrentMission = fixedMission;

        // 테스트 모드이므로, 섞여있는 리스트(RandomizedOrder)의 해당 날짜 슬롯도 
        // 강제로 이 고정 미션으로 덮어씌웁니다.
        // 이렇게 하면 UI나 DayDebugConsole이 GetMissionStrategy(day)를 호출해도 
        // 엉뚱한(섞인) 미션이 아니라, 지금 실행한 고정 미션을 반환하게 됩니다.
        if (_randomizedMissionOrder.Count == 0) InitializeMissionOrder();

        // 리스트 크기가 부족하면 채워넣기 (안전장치)
        while (_randomizedMissionOrder.Count <= targetIndex)
        {
            _randomizedMissionOrder.Add(null);
        }

        if (targetIndex < _randomizedMissionOrder.Count)
        {
            _randomizedMissionOrder[targetIndex] = fixedMission;
            Debug.Log($"<color=yellow>[Debug] Day {dayIndex} 슬롯을 고정 미션 [{fixedMission.title}]으로 덮어썼습니다.</color>");
        }

        Debug.Log($"[GameFlow] (Debug) Day {dayIndex} 고정 미션 강제 시작: {CurrentMission.title}");

        StartMissionSetup(dayIndex);
    }

    // 공통 미션 설정 로직 (중복 제거)
    private void StartMissionSetup(int dayIndex)
    {
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 설정 중...");

        // 전략 실행
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // 이상현상 배정 실행
        if (AnomalyDistributor.Instance != null)
        {
            AnomalyDistributor.Instance.DistributeAnomalies();
        }

        EventBus.Publish(new MissionStartedEvent { mission = CurrentMission });
        EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });
    }

    public void NotifyItemFound(string itemTag)
    {
        if (CurrentMission != null && CurrentMission.IsValidItem(itemTag))
        {
            CurrentScore++;
            CurrentMission.OnEventTriggered(itemTag);
            EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });
            Debug.Log($"[Mission] 목표 아이템 발견! 점수: {CurrentScore}/{CurrentMission.targetScore}");
        }
        else
        {
            Debug.Log($"[Mission] 아이템 발견({itemTag})했으나 목표 아님.");
        }
    }

    public void NotifyPrisonerResolved(string cellId)
    {
        dailyResolvedCount++;
        Debug.Log($"[GameFlow] 죄수 해결 확인! (금일 누적: {dailyResolvedCount})");

        if (CurrentMission != null && CurrentMission.IsValidPrisoner(cellId))
        {
            CurrentScore++;
            CurrentMission.OnEventTriggered("PrisonerResolved");
            EventBus.Publish(new MissionProgressChangedEvent { current = CurrentScore, target = CurrentMission.targetScore });
            Debug.Log($"[Mission] 타겟 죄수 제압 성공! 점수 증가.");
        }
        else
        {
            Debug.Log($"[Mission] 죄수 제압({cellId})했으나 타겟 아님.");
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
        if (_randomizedMissionOrder.Count == 0) InitializeMissionOrder();

        int listIndex = dayIndex - 1;
        if (listIndex >= 0 && listIndex < _randomizedMissionOrder.Count)
        {
            return _randomizedMissionOrder[listIndex];
        }

        Debug.LogWarning($"[DailyMissionManager] {dayIndex}일차 미션 데이터가 없습니다.");
        return null;
    }
    public void ResetDailyFlags()
    {
        IsBriefingCompleted = false;
        IsReported = false;
    }

    // [추가] 현재 섞인 미션들의 인덱스 리스트 추출 (저장용)
    public List<int> GetMissionOrderIndices()
    {
        List<int> indices = new List<int>();
        if (_randomizedMissionOrder == null) return indices;

        foreach (var mission in _randomizedMissionOrder)
        {
            // 원본 시나리오 리스트에서 이 미션이 몇 번째였는지 찾아서 저장
            int index = missionScenario.IndexOf(mission);
            indices.Add(index);
        }
        return indices;
    }

    // [추가] 저장된 인덱스 리스트를 받아 미션 순서 복원 (로드용)
    public void RestoreMissionOrder(List<int> savedIndices)
    {
        if (savedIndices == null || savedIndices.Count == 0) return;

        _randomizedMissionOrder.Clear();
        foreach (int index in savedIndices)
        {
            if (index >= 0 && index < missionScenario.Count)
            {
                _randomizedMissionOrder.Add(missionScenario[index]);
            }
        }
        Debug.Log("[Mission] 저장된 데이터 기반으로 미션 순서를 복원했습니다.");
    }

    private void OnMissionEndRequested(MissionEndRequestedEvent e)
    {
        if (CurrentMission is Mission_FindImposterStrategy imposter)
        {
            if (e.IsSuccess && imposter.SuccessSequence != null)
            {
                EventBus.Publish(new SequencePlayRequestedEvent
                {
                    Sequence = imposter.SuccessSequence
                });
                return;
            }
        }

        // Fail
        EvaluateDayResult(out string failReason);
        EventBus.Publish(new ResultUIShowRequestedEvent(e.IsSuccess, failReason));
    }
    // =========================================================
    // 새 게임(튜토리얼 스킵 포함)용 완전 초기화
    // =========================================================
    public void ResetAll()
    {
        Debug.Log("[DailyMissionManager] ResetAll (New Game)");

        // 현재 미션 제거
        CurrentMission = null;

        // 진행 상태 리셋
        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;

        // 미션 순서 완전 재셔플
        InitializeMissionOrder();
    }
    // =========================================================
    // 플래그 제어 API
    // =========================================================
    public void MarkBriefingCompleted()
    {
        IsBriefingCompleted = true;
    }

    public void MarkBriefingDialogueViewed() // NPC 대화 완료
    {
        IsBriefingDialogueViewed = true;
    }

    public void MarkReported()
    {
        IsReported = true;
    }
}