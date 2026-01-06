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
    [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
    public GamePhase CurrentPhase => currentPhase;
    [SerializeField] private int currentDay = 0;
    [SerializeField] private int riotGauge = 20;
    [SerializeField] private int maxRiotGauge = 100;
    [SerializeField] public int maxDay = 7;

    public int RiotGauge => riotGauge;
    public int CurrentRiotGauge => riotGauge;
    public int MaxRiotGauge => maxRiotGauge;
    public int CurrentDay => currentDay;
    public int MaxDay => maxDay;

    private Coroutine patrolTimerCoroutine;

    private Action<RequestPhaseChangeEvent> _requestPhaseChange;
    private Action<EndingConditionMetEvent> _onEndingConditionMet;

    [Header("엔딩 설정")]
    private GameEndingType finalEnding = GameEndingType.None;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<GameEndingType> OnGameEnded;

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 480f;
    public float CurrentInGameSeconds { get; private set; }
    public event Action<float> OnInGameTimeUpdated;

    // ScheduleManager 참조
    public PrisonerScheduleManager ScheduleManager;

    private int playerHP = 70;
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

            if (playerHP <= 0 && currentPhase == GamePhase.Patrol)
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding2));
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
        EventBus.Publish(new GamePhaseChangedEvent(currentPhase));
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_requestPhaseChange);
        EventBus.Subscribe(_onEndingConditionMet);
        EventBus.Subscribe<PauseGameRequestedEvent>(_ => Time.timeScale = 0f);
        EventBus.Subscribe<ResumeGameRequestedEvent>(_ => Time.timeScale = 1f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_requestPhaseChange);
        EventBus.Unsubscribe(_onEndingConditionMet);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        }

        if (currentPhase == GamePhase.Ending) return;
        EventBus.Publish(new GamePhaseChangedEvent(newPhase));
    }

    private void OnEnterNotStarted()
    {
        currentDay = 0;
        riotGauge = 20;
        playerHP = 70;
        PrisonerScheduleManager.ResetStaticData(); // 정적 데이터 초기화
    }

    private void OnEnterStandby() => currentDay++;
    private void OnEnterBriefing() => StandbyEndTrigger();

    private void OnEnterPatrol()
    {
        EventBus.Publish(new ShowTimedTextPopupEvent("순찰 시작", 1.5f));
        patrolDurationSeconds = 480;
        CurrentInGameSeconds = patrolDurationSeconds;
        EventBus.Publish(new PatrolTimerResetEvent(patrolDurationSeconds));

        var builder = FindObjectOfType<SettlementReportBuilder>();
        if (builder != null) builder.CacheRiotGaugeAtStart();

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
        while (CurrentPhase == GamePhase.Patrol && patrolDurationSeconds > 0)
        {
            patrolDurationSeconds -= Time.deltaTime;
            CurrentInGameSeconds = patrolDurationSeconds;
            OnInGameTimeUpdated?.Invoke(patrolDurationSeconds);
            yield return null;
        }
        if (patrolDurationSeconds <= 0f)
        {
            patrolDurationSeconds = 0;
            ChangePhase(GamePhase.Settlement);
        }
    }

    private IEnumerator SettlementProcessRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        EndTrigger();
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
            riotGauge = this.riotGauge,
            currentPhase = this.currentPhase,
            currentHp = this.playerHP
        };

        if (ScheduleManager != null)
        {
            // out 변수로 dailyRoles를 받아옵니다.
            ScheduleManager.ExtractDataForSave(out data.prisonerRoster, out data.dailyRoles);
        }

        return data;
    }

    public bool LoadPlayerData()
    {
        var data = _saveManager.LoadGame();
        if (data != null)
        {
            this.currentDay = data.currentDay;
            this.riotGauge = data.riotGauge;
            this.currentPhase = data.currentPhase;
            this.playerHP = data.currentHp;

            if (ScheduleManager != null)
            {
                // 로드된 dailyRoles를 매니저에 주입합니다.
                ScheduleManager.OverrideScheduleFromSave(data.prisonerRoster, data.dailyRoles);
            }

            Debug.Log("세이브 로드 및 스케줄 복원 완료");
            return true;
        }
        return false;
    }

    public void ResetTimer()
    {
        patrolDurationSeconds = 480f;
    }

    public void EndTrigger()
    {
        if (riotGauge >= maxRiotGauge)
        {
            EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding3));
        }
        else
        {
            if (currentDay >= maxDay)
            {
                if (riotGauge < 30) EventBus.Publish(new EndingConditionMetEvent(GameEndingType.HappyEnding1));
                else if (riotGauge < 90) EventBus.Publish(new EndingConditionMetEvent(GameEndingType.NomalEnding1));
                else EventBus.Publish(new EndingConditionMetEvent(GameEndingType.NomalEnding2));
            }
            else
            {
                EventBus.Publish(new RequestSceneReloadEvent());
            }
        }
    }

    public void SetRiotGauge(int value) => riotGauge = Mathf.Clamp(value, 0, maxRiotGauge);

    public void AddRiotGauge(int value)
    {
        riotGauge += value;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);
        Debug.Log($"[GM]게이지 변경: {value} 적용됨. 현재: {riotGauge}");
    }

    public void OnClickSettlementButton()
    {
        _saveManager.SaveGame(GetCurrentSaveData());
        StartCoroutine(SettlementProcessRoutine());
    }

    public void OnEnterTutorial() { }

    public void StandbyEndTrigger()
    {
        if (riotGauge >= maxRiotGauge)
            EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding1));
    }

    public void RegisterScheduleManager(PrisonerScheduleManager manager)
    {
        ScheduleManager = manager;
        Debug.Log("GameManager: 스케줄 매니저가 연결되었습니다.");
    }

    public void SetDailyTimeLimit(float seconds)
    {
        this.patrolDurationSeconds = seconds;
        // 필요하다면 UI 갱신 이벤트 즉시 발생
        EventBus.Publish(new PatrolTimerResetEvent(seconds));
        Debug.Log($"[GameManager] 오늘 제한시간 설정됨: {seconds}초");
    }
}