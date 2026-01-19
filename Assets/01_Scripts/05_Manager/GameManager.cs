using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private SaveManager _saveManager;

    [Header("페이즈 상태")]
    [SerializeField] private GamePhase initialPhase = GamePhase.NotStarted; // [TEST ONLY] 테스트 시작 페이즈
    [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
    public GamePhase CurrentPhase => currentPhase;
    private StandbyEnterReason standbyEnterReason = StandbyEnterReason.None;
    [SerializeField] private int currentDay = 0;
    [SerializeField] public int maxDay = 7;

    public int CurrentDay => currentDay;
    public int MaxDay => maxDay;
    public float PatrolDurationMax => patrolDurationSeconds;

    private Coroutine patrolTimerCoroutine;

    private Action<RequestPhaseChangeEvent> _requestPhaseChange;
    private Action<EndingConditionMetEvent> _onEndingConditionMet;

    [Header("엔딩 설정")]
    private GameEndingType finalEnding = GameEndingType.None;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<GameEndingType> OnGameEnded;

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 480f;
    private bool _patrolTimeoutHandled; // 중복 방지

    public float CurrentInGameSeconds { get; private set; }
    public event Action<float> OnInGameTimeUpdated;

    // ScheduleManager 참조
    public PrisonerScheduleManager ScheduleManager;

    private int playerHP = 100;
    public int PlayerHP
    {
        get => playerHP;
        set
        {
            int clamped = Mathf.Clamp(value, 0, 100);

            if (playerHP == clamped)
                return;

            playerHP = clamped;

            // HP 변경 이벤트 발행
            EventBus.Publish(new PlayerHpChangedEvent(playerHP));

            // =========================
            // GameOver 처리
            // =========================
            if (playerHP <= 0 && currentPhase == GamePhase.Patrol)
            {
                EventBus.Publish(new GameOverEvent());
                EventBus.Publish(new ForceExitInspectionEvent());
                return;
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _saveManager = new SaveManager();

        _requestPhaseChange = (e) =>
        {
            Debug.Log($"GameManager: 페이즈 변경 요청 받음 -> {e.TargetPhase}");
            ChangePhase(e.TargetPhase);
        };
        _onEndingConditionMet = e =>
        {
            finalEnding = e.EndingType;
            ChangePhase(GamePhase.Ending);
        };
    }

    private void Start()
    {
#if UNITY_EDITOR
        StartCoroutine(CoBootstrapInitialPhase());
#else
        ChangePhase(GamePhase.NotStarted);
#endif
    }

#if UNITY_EDITOR
    private IEnumerator CoBootstrapInitialPhase()
    {
        yield return null;
        ChangePhase(initialPhase);
    }
#endif

    // [수정] 이벤트 구독 로직 분리 (재사용 목적)
    private void RegisterSystemEvents()
    {
        EventBus.Subscribe(_requestPhaseChange);
        EventBus.Subscribe(_onEndingConditionMet);
        EventBus.Subscribe<PauseGameRequestedEvent>(OnPauseRequested);
        EventBus.Subscribe<ResumeGameRequestedEvent>(OnResumeRequested);
    }

    // [수정] 이벤트 해지 로직 분리
    private void UnregisterSystemEvents()
    {
        if (_requestPhaseChange != null) EventBus.Unsubscribe(_requestPhaseChange);
        if (_onEndingConditionMet != null) EventBus.Unsubscribe(_onEndingConditionMet);
        EventBus.Unsubscribe<PauseGameRequestedEvent>(OnPauseRequested);
        EventBus.Unsubscribe<ResumeGameRequestedEvent>(OnResumeRequested);
    }

    private void OnEnable()
    {
        RegisterSystemEvents();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnregisterSystemEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 람다 대신 메서드로 분리 (안전한 구독/해지)
    private void OnPauseRequested(PauseGameRequestedEvent e) => Time.timeScale = 0f;
    private void OnResumeRequested(ResumeGameRequestedEvent e) => Time.timeScale = 1f;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // EventBus.Clear(); 

        // GameManager는 DontDestroyOnLoad라서 연결이 끊기지 않지만,
        // 혹시 모를 중복 방지 등을 위해 재구독 로직은 유지해도 괜찮습니다.
        // 다만 Clear를 안 했다면 굳이 다시 할 필요도 없습니다.

        // 안전하게 가려면 그냥 로그와 코루틴만 남기세요.
        Debug.Log("[GameManager] 씬 로드 완료");

        StartCoroutine(CoPublishGameContextReady());
    }

    private IEnumerator CoPublishGameContextReady()
    {
        yield return null;
        PublishGameContextReady();
    }

    private void PublishGameContextReady()
    {
        Debug.Log($"[GameManager] GameContextReady | Day {currentDay}/{maxDay}, Phase={currentPhase}");

        EventBus.Publish(new GameContextReadyEvent(currentDay, maxDay, currentPhase));
        EventBus.Publish(new GamePhaseChangedEvent(currentPhase));
        EventBus.Publish(new PlayerHpChangedEvent(playerHP));
    }

    public void ChangePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        Debug.Log($"{CurrentPhase} 에서 {newPhase}로 페이즈 전환이 이루어졌습니다.");
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);

        switch (newPhase)
        {
            case GamePhase.NotStarted: OnEnterNotStarted(); break;
            case GamePhase.Standby:
                OnEnterStandby();
                StartCoroutine(WaitAndChangePhase(GamePhase.Briefing, 1.5f));
                break;
            case GamePhase.Briefing: OnEnterBriefing(); break;
            case GamePhase.Patrol: OnEnterPatrol(); break;
            case GamePhase.Settlement: OnEnterSettlement(); break;
            case GamePhase.Ending: OnEnterEnding(); break;
            case GamePhase.Tutorial: OnEnterTutorial(); break;
            case GamePhase.Test: break;
        }

        if (currentPhase == GamePhase.Ending) return;
        EventBus.Publish(new GamePhaseChangedEvent(newPhase));
    }

    // [수정] NotStarted (타이틀/초기화) 상태 진입 시
    private void OnEnterNotStarted()
    {
        currentDay = 0;
        playerHP = 100;

        // ★ [핵심] 죄수 데이터 완전 초기화 (새 게임 시 좀비 데이터 제거)
        if (ScheduleManager != null)
        {
            ScheduleManager.ResetAllSimulationData();
        }
        else
        {
            // 아직 로드되지 않았을 경우를 대비해 검색
            var sm = FindObjectOfType<PrisonerScheduleManager>();
            if (sm != null) sm.ResetAllSimulationData();
        }
    }

    public void SetStandbyEnterReason(StandbyEnterReason reason)
    {
        standbyEnterReason = reason;
    }
    private void OnEnterStandby()
    {
        if (standbyEnterReason == StandbyEnterReason.NextDay)
        {
            currentDay++;
            playerHP += 10;
        }
        else if (standbyEnterReason == StandbyEnterReason.RestartSameDay)
        {
            playerHP = 100;
        }

        standbyEnterReason = StandbyEnterReason.None;
    }

    // [수정] 브리핑 진입 시 프랭크 위치 배정 추가
    private void OnEnterBriefing()
    {
        // ★ [핵심] 현재 미션에 맞춰 프랭크 위치 배정 (셔플 대응)
        var frankManager = FindObjectOfType<FrankSpawnManager>();
        if (frankManager != null && DailyMissionManager.Instance != null)
        {
            // 섞인 미션 정보(CurrentMission)를 전달
            frankManager.SpawnFrankForMission(DailyMissionManager.Instance.CurrentMission);
        }

        StandbyEndTrigger();
    }

    private void OnEnterPatrol()
    {
        _patrolTimeoutHandled = false;
        EventBus.Publish(new ShowTimedTextPopupEvent("순찰 시작", 1.5f));
        patrolDurationSeconds = 480;
        CurrentInGameSeconds = patrolDurationSeconds;
        EventBus.Publish(new PatrolTimerResetEvent(patrolDurationSeconds));
        EventBus.Publish(new DialogueStepChangedEvent(DialogueKeys.DialogueType.Fin));

        patrolTimerCoroutine = StartCoroutine(UpdateTimer());
    }

    private void OnEnterSettlement()
    {
        if (patrolTimerCoroutine != null)
        {
            StopCoroutine(patrolTimerCoroutine);
            patrolTimerCoroutine = null;
        }
        EventBus.Publish(new SettlementStartedEvent());
    }

    private void OnEnterEnding()
    {
        Debug.Log("엔딩 페이즈 진입");
        EndingData endingData = _saveManager.LoadMeta();
        if (!endingData.unlockedEndings.Contains(finalEnding))
        {
            endingData.unlockedEndings.Add(finalEnding);
            _saveManager.SaveMeta(endingData);
        }
        OnGameEnded?.Invoke(finalEnding);
    }

    private IEnumerator UpdateTimer()
    {
        yield return new WaitForSeconds(1.0f);

        while (CurrentPhase == GamePhase.Patrol)
        {
            patrolDurationSeconds -= Time.deltaTime;

            if (patrolDurationSeconds <= 0f)
            {
                HandlePatrolTimeout();
                yield break;
            }

            CurrentInGameSeconds = patrolDurationSeconds;
            OnInGameTimeUpdated?.Invoke(patrolDurationSeconds);

            yield return null;
        }
    }
    private void HandlePatrolTimeout()
    {
        if (_patrolTimeoutHandled)
            return;

        _patrolTimeoutHandled = true;

        if (patrolTimerCoroutine != null)
        {
            StopCoroutine(patrolTimerCoroutine);
            patrolTimerCoroutine = null;
        }
        EventBus.Publish(new PatrolTimeoutEvent());
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new ResultUIShowRequestedEvent(false, "순찰 시간이 초과되었습니다."));
        Debug.Log("[GameManager] Patrol Timeout → Mission Failed");
    }
    private IEnumerator WaitAndChangePhase(GamePhase nextPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangePhase(nextPhase);
    }

    public GameSaveData GetCurrentSaveData()
    {
        var data = new GameSaveData
        {
            currentDay = this.currentDay,
            currentPhase = this.currentPhase,
            currentHp = this.playerHP
        };

        // 스케줄 데이터 저장
        if (ScheduleManager != null)
        {
            ScheduleManager.ExtractDataForSave(out data.prisonerRoster, out data.dailyRoles);
        }

        // ★ [추가] 미션 순서 저장
        if (DailyMissionManager.Instance != null)
        {
            data.randomizedMissionIndices = DailyMissionManager.Instance.GetMissionOrderIndices();
        }

        return data;
    }

    public bool LoadPlayerData()
    {
        var data = _saveManager.LoadGame();
        if (data != null)
        {
            this.currentDay = data.currentDay;
            this.currentPhase = data.currentPhase;
            this.playerHP = data.currentHp;

            // 스케줄 복원
            if (ScheduleManager != null)
            {
                ScheduleManager.OverrideScheduleFromSave(data.prisonerRoster, data.dailyRoles);
            }

            if (DailyMissionManager.Instance != null && data.randomizedMissionIndices != null)
            {
                DailyMissionManager.Instance.RestoreMissionOrder(data.randomizedMissionIndices);
            }

            Debug.Log("세이브 로드 완료 (미션 순서 포함)");
            return true;
        }
        return false;
    }

    public void ResetTimer()
    {
        patrolDurationSeconds = 480f;
    }

    public void OnClickSettlementButton()
    {
        _saveManager.SaveGame(GetCurrentSaveData());
    }

    public void OnEnterTutorial() { }

    public void StandbyEndTrigger()
    {
    }

    public void RegisterScheduleManager(PrisonerScheduleManager manager)
    {
        ScheduleManager = manager;
        Debug.Log("GameManager: 스케줄 매니저가 연결되었습니다.");
    }

    public void SetDailyTimeLimit(float seconds)
    {
        this.patrolDurationSeconds = seconds;
        EventBus.Publish(new PatrolTimerResetEvent(seconds));
        Debug.Log($"[GameManager] 오늘 제한시간 설정됨: {seconds}초");
    }
}