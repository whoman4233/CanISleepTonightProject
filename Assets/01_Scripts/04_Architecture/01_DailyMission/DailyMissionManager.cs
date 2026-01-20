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

    private List<DailyMissionStrategy> _randomizedMissionOrder = new List<DailyMissionStrategy>();

    private Action<MissionEndRequestedEvent> _onMissionEndRequested;
    private Action<GameContextReadyEvent> _onGameContextReady;
    public DailyMissionStrategy CurrentMission { get; private set; }

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

    private void OnGameContextReady(GameContextReadyEvent e)
    {
        Debug.Log($"[DailyMissionManager] GameContextReady (Day {e.CurrentDay})");
        // =====================================================
        // Day 단위 상태만 리셋
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
        Debug.Log("[Mission] 새 게임 -> 미션 테이블 생성");

        _randomizedMissionOrder.Clear();
        CurrentMission = null;

        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;

        dailyResolvedCount = 0;
        CurrentScore = 0;

        InitializeMissionOrder();
    }
    public void InitializeMissionOrder()
    {
        _randomizedMissionOrder.Clear();

        if (missionScenario == null || missionScenario.Count == 0) return;

        if (missionScenario.Count < 7)
        {
            Debug.LogError("[Mission] 미션 시나리오 개수가 7개 미만입니다! (7일차 고정 불가)");
            _randomizedMissionOrder.AddRange(missionScenario);
            ShuffleList(_randomizedMissionOrder);
            return;
        }

        var normalDays = missionScenario.GetRange(0, 6);
        ShuffleList(normalDays);
        _randomizedMissionOrder.AddRange(normalDays);
        _randomizedMissionOrder.Add(missionScenario[6]);

        Debug.Log("[Mission] 미션 순서 재설정 완료: [Day 1~6 Random] + [Day 7 Fixed]");
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
        dailyResolvedCount = 0;
        CurrentScore = 0;

        if (_randomizedMissionOrder.Count == 0)
        {
            Debug.LogError("[Mission] 미션 테이블 비었음. (새 런 초기화 누락 가능성)");
            return;
        }

        int listIndex = dayIndex - 1;

        if (listIndex < 0 || listIndex >= _randomizedMissionOrder.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차 미션을 찾을 수 없습니다! (범위 초과)");
            return;
        }

        CurrentMission = _randomizedMissionOrder[listIndex];
        StartMissionSetup(dayIndex);
    }

    public void StartFixDay(int dayIndex)
    {
        dailyResolvedCount = 0;
        CurrentScore = 0;

        int targetIndex = dayIndex - 1;

        if (missionScenario == null || targetIndex < 0 || targetIndex >= missionScenario.Count)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차에 해당하는 원본 미션 데이터가 없습니다!");
            return;
        }

        var fixedMission = missionScenario[targetIndex];
        CurrentMission = fixedMission;

        if (_randomizedMissionOrder.Count == 0) InitializeMissionOrder();

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

    // ========================================================================
    // ★ [수정됨] 실행 순서 변경: SetupDay(역할배정) -> Distribute(아이템배포)
    // ========================================================================
    private void StartMissionSetup(int dayIndex)
    {
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 설정 중...");

        // 1. [순서 변경됨] 먼저 미션 전략을 실행하여 '테마'를 설정하고 '역할(Suspicious)'을 배정합니다.
        //    (SetupDay 내부에서 PrisonerScheduleManager.AssignRolesForNewDay가 호출됨)
        if (CurrentMission != null)
        {
            CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);
        }

        // 2. [순서 변경됨] 배정된 역할(Suspicious)과 테마 정보를 바탕으로 아이템을 맵에 깝니다.
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

    public List<int> GetMissionOrderIndices()
    {
        return _randomizedMissionOrder
            .Select(m => missionScenario.IndexOf(m))
            .ToList();
    }
    public bool HasValidMissionTable =>
    _randomizedMissionOrder != null && _randomizedMissionOrder.Count > 0; //PrisonManager 로드 시 보호를 위한 장치

    public void RestoreMissionOrder(List<int> savedIndices)
    {
        if (savedIndices == null || savedIndices.Count == 0)
            return;

        // 테이블이 비어있다면 초기화 후 복원
        if (_randomizedMissionOrder.Count == 0)
        {
            Debug.Log("[Mission] Restore 이전 테이블 비어있음 → 컨테이너 초기화");
            _randomizedMissionOrder = new List<DailyMissionStrategy>();
        }

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

        EvaluateDayResult(out string failReason);
        EventBus.Publish(new ResultUIShowRequestedEvent(e.IsSuccess, failReason));
    }

    public void ResetAll()
    {
        Debug.Log("[DailyMissionManager] ResetAll (New Game)");
        CurrentMission = null;
        IsBriefingCompleted = false;
        IsBriefingDialogueViewed = false;
        IsReported = false;
        dailyResolvedCount = 0;
        CurrentScore = 0;
        InitializeMissionOrder();
    }

    public void MarkBriefingCompleted()
    {
        IsBriefingCompleted = true;
    }

    public void MarkBriefingDialogueViewed()
    {
        IsBriefingDialogueViewed = true;
    }

    public void MarkReported()
    {
        IsReported = true;
    }
}