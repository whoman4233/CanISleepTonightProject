using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowController : MonoBehaviour
{
    public static FlowController Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string playSceneName = "02_PlayScene"; // 플레이 씬
    [SerializeField] private string introSceneName = "01_IntroScene"; // 인트로(타이틀) 씬
    [SerializeField] private string loadingSceneName = "07_LoadingScene_LSG"; // 로딩씬
    [SerializeField] private string tutorialSceneName = "08_TutorialScene"; // 튜토리얼 씬
    [SerializeField] private string outroSceneName = "03_OutroScene";

    private bool isBusy = false;
    private bool outroFinished;

    private Action<RequestStartNewGameEvent> _startNewGameHandler;
    private Action<ReturnToTitleRequestedEvent> _returnToTitleHandler;
    private Action<RequestSceneReloadEvent> _reloadHandler;
    private Action<RequestGameRestartEvent> _restartHandler;
    private Action<LoadGameEvent> _loadGameHandler; // 이어하기 이벤트
    private Action<IntoPlaySceneEvent> _intoPlay;
    private Action<RequestRestartFromFailureEvent> _restartFromFailureHandler; //재시작 이벤트(튜토리얼 스킵)
    private Action<EndingConditionMetEvent> _endingConditionHandler; // 엔딩신

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _startNewGameHandler = e => StartNewGame();
            _returnToTitleHandler = e => ReturnToTitle();
            _reloadHandler = e => StartCoroutine(ReloadPlaySceneRoutine());
            _restartHandler = e => StartCoroutine(ReloadPlaySceneRoutine());
            _loadGameHandler = e => StartCoroutine(LoadGameSequence());
            _intoPlay = e => StartCoroutine(LoadActualPlaySceneRoutine());
            _restartFromFailureHandler = e => StartCoroutine(RestartFromFailureSequence());
            _endingConditionHandler = OnEndingConditionMet;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        GameManager.Instance.ChangePhase(GamePhase.NotStarted);
        Debug.Log("notstarted");
    }
#if UNITY_EDITOR
    private void Update()
    {
        //  디버깅용 – 버튼으로 즉시 Outro 진입
        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (!isBusy)
            {
                Debug.Log("[Debug] Force Outro Trigger");
                StartCoroutine(PlayOutroSequence());
            }
        }
    }
#endif
    private void OnEnable()
    {
        EventBus.Subscribe(_startNewGameHandler);
        EventBus.Subscribe(_returnToTitleHandler);
        EventBus.Subscribe(_reloadHandler);
        EventBus.Subscribe(_restartHandler);
        EventBus.Subscribe(_loadGameHandler);
        EventBus.Subscribe(_intoPlay);
        EventBus.Subscribe(_restartFromFailureHandler);
        EventBus.Subscribe(_endingConditionHandler);
    }
    private void OnDisable()
    {

        EventBus.Unsubscribe(_startNewGameHandler);
        EventBus.Unsubscribe(_returnToTitleHandler);
        EventBus.Unsubscribe(_reloadHandler);
        EventBus.Unsubscribe(_restartHandler);
        EventBus.Unsubscribe(_loadGameHandler);
        EventBus.Unsubscribe(_intoPlay);
        EventBus.Unsubscribe(_restartFromFailureHandler);
        EventBus.Unsubscribe(_endingConditionHandler);
    }

    private IEnumerator LoadGameSequence()
    {
        if (isBusy) yield break;
        isBusy = true;

        // 1. 세이브 데이터 로드
        bool loaded = GameManager.Instance.LoadPlayerData();
        if (!loaded)
        {
            Debug.LogWarning("LoadGame 실패: 세이브 데이터 없음");
            EventBus.Publish(new ShowTimedTextPopupEvent("저장된 데이터가 없습니다.", 1f));
            isBusy = false;
            yield break;
        }
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드
        EventBus.Publish(new LoadingOverlayShownEvent()); //UI 숨기기 이벤트

        // 3. IntroScene 언로드
        Scene introScene = SceneManager.GetSceneByName(introSceneName);
        if (introScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(introScene);

        // 2. PlayScene 로딩 (NewGame과 동일)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) yield return null;

        Scene playScene = SceneManager.GetSceneByName(playSceneName);
        if (playScene.IsValid())
            SceneManager.SetActiveScene(playScene);

        if (DailyMissionManager.Instance != null)
        {
            var dm = DailyMissionManager.Instance;
            var save = new SaveManager().LoadGame();

            // ===============================
            // 런 미션 테이블 복구
            // ===============================
            var order = GameManager.Instance.ConsumePendingMissionOrder();
            if (order != null && order.Count > 0)
            {
                dm.RestoreMissionOrder(order);
            }

            // ===============================
            // 오늘 미션 복구
            // ===============================
            if (save != null && save.isMissionInProgress)
            {
                dm.RestoreCurrentMission(save.currentMissionIndex);
            }
        }

        // 4. 저장된 Phase로 재진입
        var phase = GameManager.Instance.CurrentPhase;

        if (phase == GamePhase.NotStarted || phase == GamePhase.Settlement)
        {
            phase = GamePhase.Standby;
        }

        GameManager.Instance.ChangePhase(phase);
        EventBus.Publish(new GameContextReadyEvent());
        yield return SceneManager.UnloadSceneAsync(loadingSceneName); // 로딩 씬 언로드
        EventBus.Publish(new LoadingOverlayHiddenEvent()); //UI 노출 이벤트

        isBusy = false;
        Debug.Log("이어하기 완료");
    }

    private IEnumerator ReloadPlaySceneRoutine() // 씬 재로딩 코루틴
    {
        isBusy = true;

        // =========================
        // [추가] 씬 리로드 전에 UI/Input 강제 리셋
        // =========================
        EventBus.Publish(new UIHardResetEvent());
        EventBus.Publish(new InputHardResetEvent());

        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        Scene playScene = SceneManager.GetSceneByName(playSceneName);
        if (playScene.isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩씬 로드
            EventBus.Publish(new LoadingOverlayShownEvent()); //UI 숨기기 이벤트
            yield return SceneManager.UnloadSceneAsync(playScene); // 현재 씬 언로드
        }
        yield return Resources.UnloadUnusedAssets(); // 메모리 정리
        System.GC.Collect(); // 메모리 정리

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive); // 플레이 씬 다시 로드
        while (!asyncLoad.isDone) yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(playSceneName)); // 씬 활성화
        yield return null; //new WaitForSeconds(1.0f);

        var dm = DailyMissionManager.Instance;
        if (dm != null)
        {
            // =========================================================
            // 1) 런 테이블이 비어있으면 -> 세이브에서 먼저 복원
            // =========================================================
            if (!dm.HasValidRunMissionTable)
            {
                // SaveManager는 경량이므로 여기서 직접 로드해도 됨
                var save = new SaveManager().LoadGame();

                if (save != null && save.randomizedMissionIndices != null && save.randomizedMissionIndices.Count > 0)
                {
                    dm.RestoreMissionOrder(save.randomizedMissionIndices);
                    Debug.Log("[FlowController] 런 미션 테이블을 세이브에서 복원했습니다.");
                }
                else
                {
                    Debug.LogWarning("[FlowController] 세이브에 런 미션 테이블이 없어 새 런 테이블을 생성합니다.");
                    dm.CreateNewMissionTableForNewRun(); // 최후 방어(원하면 제거 가능)
                }
            }

            // =========================================================
            // 2) 런 테이블은 있는데 remaining만 비었으면 -> 런 테이블 기반 복구
            // =========================================================
            if (!dm.HasRemainingMission)
            {
                dm.RestoreRemainingFromRunTable();
            }
        }
        GameManager.Instance.ChangePhase(GamePhase.Standby); // 페이즈 전환
        isBusy = false;
        //yield return new WaitForSeconds(0.5f); // 추가로 0.5초 로딩화면 보여줌 추후 브리핑 페이즈에 로딩씬 끝나게?
        yield return SceneManager.UnloadSceneAsync(loadingSceneName); // 로딩 씬 언로드
        EventBus.Publish(new LoadingOverlayHiddenEvent()); //UI 노출 이벤트
        Debug.Log("씬 재로딩 완료");
    }

    public void StartNewGame()
    {
        if (isBusy) return;

        // 새 런 시작 명시
        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.CreateNewMissionTableForNewRun();
        }

        StartCoroutine(LoadPlaySceneSequence());
    }


    private IEnumerator LoadPlaySceneSequence()
    {
        isBusy = true;
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드
        EventBus.Publish(new LoadingOverlayShownEvent()); //UI 숨기기 이벤트
        // 튜토리얼 씬 로드
        yield return SceneManager.LoadSceneAsync(tutorialSceneName, LoadSceneMode.Additive);
        Scene tutorialScene = SceneManager.GetSceneByName(tutorialSceneName);
        if (tutorialScene.IsValid()) SceneManager.SetActiveScene(tutorialScene);

        Scene introScene = SceneManager.GetSceneByName(introSceneName);
        if (introScene.isLoaded) yield return SceneManager.UnloadSceneAsync(introScene); // 인트로씬 언로드

        GameManager.Instance.ChangePhase(GamePhase.Tutorial);
        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        if (loadingScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(loadingScene); // 로딩 씬 언로드
            EventBus.Publish(new LoadingOverlayHiddenEvent()); //UI 노출 이벤트
        }

        isBusy = false;
    }

    private IEnumerator LoadActualPlaySceneRoutine()
    {
        isBusy = true;
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);

        EventBus.Publish(new LoadingOverlayShownEvent());

        Scene tutorialScene = SceneManager.GetSceneByName(tutorialSceneName);
        if (tutorialScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(tutorialScene);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) yield return null;

        Scene loadedPlayScene = SceneManager.GetSceneByName(playSceneName);
        if (loadedPlayScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedPlayScene);
        }

        GameManager.Instance.ResetTimer();

        // =====================================================
        // "새 게임 루트"에서만 미션 테이블 생성
        // - CurrentDay 조건 제거
        // - StartNewGame / RestartFromFailure 에서만 호출되도록 책임 이동
        // =====================================================

        if (DailyMissionManager.Instance != null && !DailyMissionManager.Instance.HasValidRunMissionTable)
        {
            DailyMissionManager.Instance.CreateNewMissionTableForNewRun();
        }

        GameManager.Instance.ChangePhase(GamePhase.Standby);

        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        if (loadingScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(loadingScene);
            EventBus.Publish(new LoadingOverlayHiddenEvent());
        }

        isBusy = false;
    }

    public void ReturnToTitle()
    {
        if (isBusy) return;
        CleanUpSystemBeforeSceneLoad();
        StartCoroutine(ReturnToTitleSequence());
    }
    private IEnumerator ReturnToTitleSequence()
    {
        isBusy = true;
        Time.timeScale = 1f;

        // =========================
        // 타이틀 복귀 전에 UI/Input 강제 리셋
        // =========================
        EventBus.Publish(new UIHardResetEvent());
        EventBus.Publish(new InputHardResetEvent());

        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드
        EventBus.Publish(new LoadingOverlayShownEvent()); //UI 숨기기 이벤트
        // 1. 현재 로드된 'PlayScene'만 비동기로 언로드합니다.
        Scene playScene = SceneManager.GetSceneByName(playSceneName);
        if (playScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(playScene);
        }

        Scene tutorialScene = SceneManager.GetSceneByName(tutorialSceneName);
        if (tutorialScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(tutorialScene);
        }

        // 2. 'IntroScene'을 Additive로 로드합니다. (Single이 아님!)
        if (!SceneManager.GetSceneByName(introSceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(introSceneName, LoadSceneMode.Additive);
        }

        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        if (loadingScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(loadingScene); // 로딩 씬 언로드
            EventBus.Publish(new LoadingOverlayHiddenEvent()); //UI 노출 이벤트
        }

        // 3. 인트로 씬을 메인으로 설정
        Scene intro = SceneManager.GetSceneByName(introSceneName);
        SceneManager.SetActiveScene(intro);

        // 4. 상태 초기화
        GameManager.Instance.ChangePhase(GamePhase.NotStarted);

        isBusy = false;
    }
    public void EnterPlayFromTutorial() // 튜토리얼에서 플레이씬 진입
    {
        if (!isBusy) StartCoroutine(LoadActualPlaySceneRoutine());
    }

    // =========================================================
    // 근무 실패 → 새 게임(튜토리얼 스킵) 시퀀스
    // =========================================================
    private IEnumerator RestartFromFailureSequence()
    {
        CleanUpSystemBeforeSceneLoad();
        if (isBusy) yield break;
        isBusy = true;

        Time.timeScale = 1f;

        EventBus.Publish(new UIHardResetEvent());
        EventBus.Publish(new InputHardResetEvent());

        //GameManager만 먼저 리셋
        GameManager.Instance.ResetForNewGameSkipTutorial();

        // 로딩 씬
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
        EventBus.Publish(new LoadingOverlayShownEvent());

        UnloadIfLoaded(playSceneName);
        UnloadIfLoaded(tutorialSceneName);
        UnloadIfLoaded(introSceneName);

        // PlayScene 로드
        yield return SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(playSceneName));

        // 여기서부터 DailyMissionManager가 존재함
        yield return null; // 한 프레임 대기 (Awake 보장)

        if (DailyMissionManager.Instance != null)
        {
            // 새 런이므로 무조건 새 테이블
            DailyMissionManager.Instance.CreateNewMissionTableForNewRun();
        }
        else
        {
            Debug.LogError("[RestartFromFailure] DailyMissionManager 생성 실패");
        }

        // 미션 테이블 생성 이후에 Standby 진입
        GameManager.Instance.ChangePhase(GamePhase.Standby);

        yield return SceneManager.UnloadSceneAsync(loadingSceneName);
        EventBus.Publish(new LoadingOverlayHiddenEvent());

        isBusy = false;
        Debug.Log("근무 실패 → 새 게임(튜토리얼 스킵) 완료");
    }

    private void UnloadIfLoaded(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
    }

    private void CleanUpSystemBeforeSceneLoad()
    {
        // 대화 시스템 강제 초기화
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue(); // 열려있는 창 닫기
        }

        // 이벤트 버스 클리어
        EventBus.ClearLocalEvents();
        // 시간 축 초기화
        Time.timeScale = 1f;
    }

    //LoadSceneAsync = 씬이 로딩되는 동안에도 백그라운드에서 다른 연산(로딩 바 갱신, 팁 출력 등)가능, yield return null을 통해 로딩이 완전히 완료될 때까지 안전하게 기다린 후 다음 코드를 실행.
    //isBusy = 로딩이 진행 중일 때는 추가적인 로딩 요청을 무시
    //SceneManager.SetActiveScene을 통해 새로 불러온 씬을 메인으로 설정
    //전역화 시켜서 게임 종료 시까지 컨트롤러가 모든 씬 전환을 책임짐

    // =========================
    // 엔딩 씬 메서드
    // =========================
    private IEnumerator PlayOutroSequence()
    {
        isBusy = true;
        Time.timeScale = 1f;

        EventBus.Publish(new UIHardResetEvent());
        EventBus.Publish(new InputHardResetEvent());

        // =========================
        // 로딩 씬 ON
        // =========================
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
        EventBus.Publish(new LoadingOverlayShownEvent());

        UnloadIfLoaded(playSceneName);
        UnloadIfLoaded(tutorialSceneName);

        // =========================
        // Outro 로드
        // =========================
        yield return SceneManager.LoadSceneAsync(outroSceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(outroSceneName));

        // 로딩 씬 OFF
        yield return SceneManager.UnloadSceneAsync(loadingSceneName);
        EventBus.Publish(new LoadingOverlayHiddenEvent());

        // =========================
        // Timeline 종료 대기
        // =========================
        outroFinished = false;
        EventBus.Subscribe<OutroFinishedEvent>(OnOutroFinished);

        while (!outroFinished)
            yield return null;

        EventBus.Unsubscribe<OutroFinishedEvent>(OnOutroFinished);

        // =========================
        // Outro 종료 → Intro
        // =========================
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
        EventBus.Publish(new LoadingOverlayShownEvent());

        yield return SceneManager.UnloadSceneAsync(outroSceneName);

        if (!SceneManager.GetSceneByName(introSceneName).isLoaded)
            yield return SceneManager.LoadSceneAsync(introSceneName, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(introSceneName));

        yield return SceneManager.UnloadSceneAsync(loadingSceneName);
        EventBus.Publish(new LoadingOverlayHiddenEvent());

        GameManager.Instance.ChangePhase(GamePhase.NotStarted);

        isBusy = false;
    }


    private void OnEndingConditionMet(EndingConditionMetEvent e)
    {
        if (isBusy)
            return;

        StartCoroutine(PlayOutroSequence());
    }
    private IEnumerator WaitForOutroFinished()
    {
        outroFinished = false;

        EventBus.Subscribe<OutroFinishedEvent>(OnOutroFinished);

        while (!outroFinished)
            yield return null;

        EventBus.Unsubscribe<OutroFinishedEvent>(OnOutroFinished);
    }

    private void OnOutroFinished(OutroFinishedEvent e)
    {
        outroFinished = true;
    }
}