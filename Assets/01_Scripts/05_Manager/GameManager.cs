using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private SaveManager _saveManager;
    public bool IsTimerPaused { get; set; } = false; // 타임어택 멈추게 하는 변수

    [Header("페이즈 상태")]
    [SerializeField] private GamePhase initialPhase = GamePhase.NotStarted; // [TEST ONLY] 테스트 시작 페이즈
    [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
    public GamePhase CurrentPhase => currentPhase;
    private StandbyEnterReason standbyEnterReason = StandbyEnterReason.None;

    [SerializeField] private int currentDay = 0;
    [SerializeField] public int maxDay = 7;

    // ★ [추가] 무사고 날짜 추적 변수
    public int CurrentAccidentFreeDay { get; private set; } = 0;

    public int CurrentDay => currentDay;
    public int MaxDay => maxDay;
    public float PatrolDurationMax => patrolDurationSeconds;

    private Coroutine patrolTimerCoroutine;

    private Action<RequestPhaseChangeEvent> _requestPhaseChange;

    [Header("엔딩 설정")]
    private bool isEndingTriggered;

    private GameEndingType finalEnding = GameEndingType.None;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<GameEndingType> OnGameEnded;

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 480f;
    private float _remainingPatrolSeconds;
    private bool _patrolTimeoutHandled; // 중복 방지

    public float CurrentInGameSeconds { get; private set; }
    public event Action<float> OnInGameTimeUpdated;
    public List<int> PendingMissionOrder { get; private set; } //MissionTable

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
        EventBus.Subscribe<PauseGameRequestedEvent>(OnPauseRequested);
        EventBus.Subscribe<ResumeGameRequestedEvent>(OnResumeRequested);
    }

    // [수정] 이벤트 해지 로직 분리
    private void UnregisterSystemEvents()
    {
        if (_requestPhaseChange != null) EventBus.Unsubscribe(_requestPhaseChange);
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
        // 무사고일 갱신 이벤트 필요시 발행
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
        CurrentAccidentFreeDay = 0; // ★ 완전 초기화 시 무사고일도 리셋

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
        // 1. 정상적으로 다음 날로 넘어가는 경우 (성공)
        if (standbyEnterReason == StandbyEnterReason.NextDay)
        {
            // =========================
            // 엔딩 판단
            // =========================
            if (currentDay + 1 >= maxDay)
            {
                Debug.Log("[GameManager] 마지막 날 진입 -> 엔딩 신");

                EnterEnding(GameEndingType.NormalEnding);
                return;
            }

            currentDay++;
            Debug.Log("Day++");
            //playerHP = Mathf.Min(playerHP + 10, 100);
            playerHP = 100;
            CurrentAccidentFreeDay++; // 다음날로 넘어가면 무사고 +1

            // 7일차 도달 시 엔딩 트리거
            if (currentDay > maxDay)
            {
                EventBus.Publish(new EndingConditionMetEvent
                {
                    EndingType = GameEndingType.NormalEnding // 필요 시 분기 가능
                });
                return;
            }

        }
        _saveManager.SaveGame(GetCurrentSaveData());

        Debug.Log($"[Save] Standby 진입 시 자동 저장 (Day {currentDay})");

        standbyEnterReason = StandbyEnterReason.None;
    }

    // [수정] 브리핑 진입 시 프랭크 위치 배정 추가
    private void OnEnterBriefing()
    {
        // ★ [핵심] 현재 미션에 맞춰 프랭크 위치 배정 (셔플 대응)
        //var frankManager = FindObjectOfType<FrankSpawnManager>();
        //if (frankManager != null && DailyMissionManager.Instance != null)
        //{
        //    // 섞인 미션 정보(CurrentMission)를 전달
        //    frankManager.SpawnFrankForMission(DailyMissionManager.Instance.CurrentMission);
        //}

        StandbyEndTrigger();
    }

    private void OnEnterPatrol()
    {
        IsTimerPaused = false;
        _patrolTimeoutHandled = false;
        EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_52", 1.5f));
        //patrolDurationSeconds = 480;
        _remainingPatrolSeconds = patrolDurationSeconds;
        CurrentInGameSeconds = patrolDurationSeconds;
        //EventBus.Publish(new PatrolTimerResetEvent(patrolDurationSeconds));
        EventBus.Publish(new DialogueStepChangedEvent(DialogueKeys.DialogueType.Fin));

        patrolTimerCoroutine = StartCoroutine(UpdateTimer());
    }

    private void OnEnterSettlement()
    {
        Debug.Log("EnterSettlement");
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
    // =========================
    // 엔딩 전용 진입 메서드
    // =========================
    public void EnterEnding(GameEndingType endingType)
    {
        if (isEndingTriggered)
            return;

        isEndingTriggered = true;
        finalEnding = endingType;

        EventBus.Publish(new EndingConditionMetEvent
        {
            EndingType = endingType
        });
    }

    private IEnumerator UpdateTimer()
    {
        yield return new WaitForSeconds(1.0f);

        while (CurrentPhase == GamePhase.Patrol)
        {
            if (!IsTimerPaused)
            {
                _remainingPatrolSeconds -= Time.deltaTime;

                if (_remainingPatrolSeconds <= 0f)
                {
                    HandlePatrolTimeout();
                    yield break;
                }

                CurrentInGameSeconds = _remainingPatrolSeconds;
                OnInGameTimeUpdated?.Invoke(_remainingPatrolSeconds);
            }
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
    public void SaveNow()
    {
        _saveManager.SaveGame(GetCurrentSaveData());
        Debug.Log("[Save] SaveNow 호출됨 (미션 확정 저장)");
    }
    public GameSaveData GetCurrentSaveData()
    {
        var data = new GameSaveData
        {
            currentDay = this.currentDay,
            currentPhase = this.currentPhase,
            currentHp = this.playerHP,
            // accidentFreeDay = this.CurrentAccidentFreeDay 
        };

        // 스케줄 데이터 저장
        if (ScheduleManager != null)
        {
            ScheduleManager.ExtractDataForSave(out data.prisonerRoster, out data.dailyRoles);
        }

        // 미션 순서 저장
        if (DailyMissionManager.Instance != null)
        {
            data.randomizedMissionIndices = DailyMissionManager.Instance.GetMissionOrderIndices();

            var dm = DailyMissionManager.Instance;
            if (dm.CurrentMission != null)
            {
                data.currentMissionIndex =
                    dm.GetMissionIndex(dm.CurrentMission);

                data.isMissionInProgress = true;
            }
            else
            {
                data.currentMissionIndex = -1;
                data.isMissionInProgress = false;
            }
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

            // ★ 무사고일 복원 (필드 있다면)
            // this.CurrentAccidentFreeDay = data.accidentFreeDay;

            // 스케줄 복원
            if (ScheduleManager != null)
            {
                ScheduleManager.OverrideScheduleFromSave(data.prisonerRoster, data.dailyRoles);
            }

            PendingMissionOrder = data.randomizedMissionIndices;

            Debug.Log("세이브 로드 완료 (미션 순서 포함)");
            return true;
        }
        return false;
    }
    public List<int> ConsumePendingMissionOrder() //미션테이블 불러오기 용
    {
        var temp = PendingMissionOrder;
        PendingMissionOrder = null;
        return temp;
    }
    // ★ [추가] 미션 실패 시 호출될 재시작 메서드
    public void RetryGameFromFailure()
    {
        Debug.Log("[GameManager] 미션 실패 -> 해당 일차 재시작 (무사고 기록 초기화)");

        // 1. 무사고 기록은 깨졌으므로 0으로 초기화
        CurrentAccidentFreeDay = 0;

        // 2. 날짜(currentDay)는 유지하되, 체력 등은 초기화
        playerHP = 100;

        // 3. 재시작 이유 설정 (같은 날 재시작)
        standbyEnterReason = StandbyEnterReason.RestartSameDay;

        // 4. 씬 리로드 (데이터는 유지된 상태로 씬 다시 시작)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // 주의: 씬이 로드되면 GameManager는 DontDestroyOnLoad라 유지되지만, 
        // Start() 로직 등에 의해 NotStarted로 초기화되지 않도록 주의해야 함.
        // 현재 코드상 씬 로드 시 CoPublishGameContextReady만 호출하므로 데이터는 유지됨.
    }

    // ★ [추가] 게임 오버 후 완전히 1일차로 돌아가려면 이 함수 사용
    public void RestartGameCompletely()
    {
        Debug.Log("[GameManager] 게임 완전 재시작 (Reset to Day 0)");

        // 1. 모든 데이터 초기화
        currentDay = 0;
        playerHP = 100;
        CurrentAccidentFreeDay = 0;

        // 2. 초기화 페이즈로 전환
        ChangePhase(GamePhase.NotStarted);

        // 3. 씬 리로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
    public void ResetForNewGameSkipTutorial()
    {
        Debug.Log("[GameManager] ResetForNewGameSkipTutorial");

        currentDay = 0;
        playerHP = 100;
        CurrentAccidentFreeDay = 0;
        standbyEnterReason = StandbyEnterReason.None;

        if (ScheduleManager != null)
            ScheduleManager.ResetAllSimulationData();
    }
}