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
    [SerializeField] private int riotGauge = 10;
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

    public event Action<GamePhase> OnPhaseChanged; // 페이즈 변경 이벤트
    public event Action<GameEndingType> OnGameEnded; // 게임엔딩 이벤트

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 480f; // 480초
    public float CurrentInGameSeconds { get; private set; } //Timer HUD 참조할 값
    public event Action<float> OnInGameTimeUpdated; // 타이머 관련 ui이벤트 

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
            Debug.Log($"GameManager: 페이즈 변경 요청 받음 -> {e.TargetPhase}"); ChangePhase(e.TargetPhase);
        };// 페이즈 변경 요청시 바로 변경
        _onEndingConditionMet = e => // 엔딩 조건 충족하면 엔딩페이즈 이동 및 엔딩타입 받아옴
        {
            finalEnding = e.EndingType;
            ChangePhase(GamePhase.Ending);
        };

    }
    //public void Initialize() // Bootstrap에서 GameContext.RegisterService<GameManager>(this) 이후 호출
    //{
    //    StartCoroutine(StartFirstPhase());
    //}

    private void Start()
    {
        // IntroScene에서 최초 1회 UI 동기화용
        EventBus.Publish(new GamePhaseChangedEvent(currentPhase));
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_requestPhaseChange);
        EventBus.Subscribe(_onEndingConditionMet);

        //인게임 메뉴 팝업시 시간정지
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
        // 씬 재로딩 후, 씬 종속 매니저들이 모두 생성된 다음
        StartCoroutine(CoPublishGameContextReady());
    }

    private IEnumerator CoPublishGameContextReady()
    {
        yield return null; // 중요: 1프레임 대기

        PublishGameContextReady();
    }

    private void PublishGameContextReady() //게임 루프 체크용(날짜로 확인)
    {
        Debug.Log($"[GameManager] GameContextReady | Day {currentDay}/{maxDay}, Phase={currentPhase}");

        EventBus.Publish(new GameContextReadyEvent(currentDay, maxDay, currentPhase));

        // 중요: 씬 리로드/재진입 시 DDOL UI들이 현재 페이즈를 다시 적용할 수 있도록
        EventBus.Publish(new GamePhaseChangedEvent(currentPhase));
    }
    public void ChangePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        Debug.Log($"{CurrentPhase} 에서 {newPhase}로 페이즈 전환이 이루어졌습니다.");
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);

        // =========================
        // [추가] UI 및 외부 시스템 전파용
        // =========================

        switch (newPhase)
        {
            case GamePhase.NotStarted:
                OnEnterNotStarted();
                break;
            case GamePhase.Standby:
                OnEnterStandby();
                StartCoroutine(WaitAndChangePhase(GamePhase.Briefing, 1.5f)); // 1.5초 후 자동으로 브리핑페이즈로 전환
                break;
            case GamePhase.Briefing:
                OnEnterBriefing();
                //StartCoroutine(WaitAndChangePhase(GamePhase.Patrol, 1.5f));
                break;
            case GamePhase.Patrol:
                OnEnterPatrol();
                break;
            case GamePhase.Settlement:
                OnEnterSettlement();
                break;
            case GamePhase.Ending:
                OnEnterEnding();
                break;
            case GamePhase.Tutorial:
                OnEnterTutorial();
                break;
            default:
                break;
        }
        if (currentPhase == GamePhase.Ending)
        {
            return;
        }
        EventBus.Publish(new GamePhaseChangedEvent(newPhase));
    }

    private void OnEnterNotStarted() // 루프 시 초기화 및 메인으로(인트로씬) 돌아가면 초기화
    {
        currentDay = 0;
        riotGauge = 10;
    }
    private void OnEnterStandby() // 준비 페이즈
    {
        currentDay++;
    }
    private void OnEnterBriefing() // 브리핑 페이즈
    {

    }

    private void OnEnterPatrol() // 순찰 페이즈
    {
        EventBus.Publish(new ShowTimedTextPopupEvent("순찰 시작", 1.5f));

        patrolDurationSeconds = 480;

        CurrentInGameSeconds = patrolDurationSeconds;

        EventBus.Publish(new PatrolTimerResetEvent(patrolDurationSeconds)); // UI 시간초기화 (기존 페이즈의 잔존시간 보이지 않도록)

        // =========================
        // RiotGauge 기준값 캐싱 (추가)
        // =========================
        var builder = FindObjectOfType<SettlementReportBuilder>();
        if (builder != null)
        {
            builder.CacheRiotGaugeAtStart();
        }

        patrolTimerCoroutine = StartCoroutine(UpdateTimer()); // 새 타이머 코루틴 시작
    }

    private void OnEnterSettlement() // 정산 페이즈
    {
        if (patrolTimerCoroutine != null) // 기존 타이머 코루틴 없애줌
        {
            StopCoroutine(patrolTimerCoroutine);
            patrolTimerCoroutine = null;
        }
        EventBus.Publish(new SettlementStartedEvent());
        //StartCoroutine(SettlementProcessRoutine());
    }

    private void OnEnterEnding() // 엔딩 페이즈
    {
        //추후 엔딩 연출 추가
        Debug.Log("엔딩 페이즈 진입");
        Debug.Log($"{finalEnding}에 진입하였습니다.");
        EndingData endingData = _saveManager.LoadMeta(); // loadmeta에 있는 if (!File.Exists(path)) return new EndingData();가 엔딩데이터가 없으면 새로 만들어줌
        if (!endingData.unlockedEndings.Contains(finalEnding)) // 엔딩 수집 정보에 현재 엔딩이 없으면
        {
            endingData.unlockedEndings.Add(finalEnding); // 추가해주고
            _saveManager.SaveMeta(endingData); // 세이브해줌
            Debug.Log($"새로운 엔딩이 저장되었습니다 {finalEnding}");
        }
        OnGameEnded?.Invoke(finalEnding);
    }

    private IEnumerator UpdateTimer() // 타이머 코루틴
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
    private IEnumerator SettlementProcessRoutine() // 다른 매니저들 초기화 기다림
    {
        yield return new WaitForSeconds(1.0f);
        _saveManager.SaveGame(GetCurrentSaveData()); // 퇴근페이즈에서 오토세이브
        EndTrigger();
    }

    private IEnumerator WaitAndChangePhase(GamePhase nextPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangePhase(nextPhase);
    }
    public GameSaveData GetCurrentSaveData()
    {
        return new GameSaveData
        {
            currentDay = this.currentDay,
            riotGauge = this.riotGauge,
            currentPhase = this.currentPhase
        };
    }

    public bool LoadPlayerData() // 이어하기 추가 시 호출 될 함수
    {
        var data = _saveManager.LoadGame();
        if (data != null)
        {
            this.currentDay = data.currentDay;
            this.riotGauge = data.riotGauge;
            this.currentPhase = data.currentPhase;
            Debug.Log("세이브 파일을 성공적으로 불러왔습니다.");
            return true;
        }
        Debug.LogWarning("세이브 파일이 없습니다");
        return false;
    }

    public void ResetTimer()
    {
        patrolDurationSeconds = 480f;
        Debug.Log("타이머가 480초로 초기화되었습니다.");
    }

    public void EndTrigger()
    {
        if (riotGauge >= maxRiotGauge)
        {
            if (currentDay < maxDay)
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding2)); // 산업 재해(7일 이전에 폭동 100 이상)
                Debug.Log("BadEnding2");
            }
            else
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding3)); // 위기 회피(7일차에 폭동 100 이상으로 퇴근)
                Debug.Log("BadEnding3");
            }
        }
        else
        {
            if(currentDay >= maxDay)
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.HappyEnding1)); // 7일차까지 무사히 완료
            }
            else
            {
                EventBus.Publish(new RequestSceneReloadEvent());
            }
        }
    }

    public void SetRiotGauge(int value)
    {
        riotGauge = Mathf.Clamp(value, 0, maxRiotGauge);
    }
    public void AddRiotGauge(int value)
    {
        riotGauge += value;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);

        Debug.Log($"[GM]게이지 변경: {value} 적용됨. 현재: {riotGauge}");
    }


    public void OnClickSettlementButton() // 보고서 nextday 버튼 클릭 시 호출 될 함수
    {
        StartCoroutine(SettlementProcessRoutine());
    }

    public void OnEnterTutorial()
    {

    }
}
