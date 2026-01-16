using System;
using System.Collections;
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

    private bool isBusy = false;

    private Action<RequestStartNewGameEvent> _startNewGameHandler;
    private Action<ReturnToTitleRequestedEvent> _returnToTitleHandler;
    private Action<RequestSceneReloadEvent> _reloadHandler;
    private Action<LoadGameEvent> _loadGameHandler; // 이어하기 이벤트

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _startNewGameHandler = e => StartNewGame();
            _returnToTitleHandler = e => ReturnToTitle();
            _reloadHandler = e => StartCoroutine(ReloadPlaySceneRoutine());
            _loadGameHandler = e => StartCoroutine(LoadGameSequence());
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

    private void OnEnable()
    {
        EventBus.Subscribe(_startNewGameHandler);
        EventBus.Subscribe(_returnToTitleHandler);
        EventBus.Subscribe(_reloadHandler);
        EventBus.Subscribe(_loadGameHandler);
    }
    private void OnDisable()
    {

        EventBus.Unsubscribe(_startNewGameHandler);
        EventBus.Unsubscribe(_returnToTitleHandler);
        EventBus.Unsubscribe(_reloadHandler);
        EventBus.Unsubscribe(_loadGameHandler);
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
        SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드

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

        // 4. 저장된 Phase로 재진입
        var phase = GameManager.Instance.CurrentPhase;

        if (phase == GamePhase.NotStarted || phase == GamePhase.Settlement)
        {
            phase = GamePhase.Standby;
        }

        GameManager.Instance.ChangePhase(phase);
        SceneManager.UnloadSceneAsync(loadingSceneName); // 로딩 씬 언로드

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
            yield return SceneManager.UnloadSceneAsync(playScene); // 현재 씬 언로드
        }
        yield return Resources.UnloadUnusedAssets(); // 메모리 정리
        System.GC.Collect(); // 메모리 정리

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive); // 플레이 씬 다시 로드
        while (!asyncLoad.isDone) yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(playSceneName)); // 씬 활성화
        yield return null; //new WaitForSeconds(1.0f);
        GameManager.Instance.ChangePhase(GamePhase.Standby); // 페이즈 전환
        isBusy = false;
        //yield return new WaitForSeconds(0.5f); // 추가로 0.5초 로딩화면 보여줌 추후 브리핑 페이즈에 로딩씬 끝나게?
        SceneManager.UnloadSceneAsync(loadingSceneName); // 로딩 씬 언로드
        Debug.Log("씬 재로딩 완료");
    }

    public void StartNewGame()
    {
        if (isBusy) return;
        StartCoroutine(LoadPlaySceneSequence());
    }

    private IEnumerator LoadPlaySceneSequence()
    {
        isBusy = true;
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드
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
        }

        isBusy = false;
    }

    private IEnumerator LoadActualPlaySceneRoutine() // 튜토리얼 이후 플레이 진입
    {
        isBusy = true;
        yield return SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive); // 로딩 씬 로드

        Scene tutorialScene = SceneManager.GetSceneByName(tutorialSceneName);
        if (tutorialScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(tutorialScene); // 튜토리얼 씬 언로드
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive); // 플레이씬 비동기 로드
        while (!asyncLoad.isDone) yield return null;

        Scene loadedPlayScene = SceneManager.GetSceneByName(playSceneName);
        if (loadedPlayScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedPlayScene); // 플레이 씬 활성화
        }

        GameManager.Instance.ResetTimer();

        GameManager.Instance.ChangePhase(GamePhase.Standby); // 페이즈 변경

        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        if (loadingScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(loadingScene); // 로딩 씬 언로드
        }

        isBusy = false;
        Debug.Log($"{playSceneName} 전환 완료 및 Standby 진입");
    }
    public void ReturnToTitle()
    {
        if (isBusy) return;
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



    //LoadSceneAsync = 씬이 로딩되는 동안에도 백그라운드에서 다른 연산(로딩 바 갱신, 팁 출력 등)가능, yield return null을 통해 로딩이 완전히 완료될 때까지 안전하게 기다린 후 다음 코드를 실행.
    //isBusy = 로딩이 진행 중일 때는 추가적인 로딩 요청을 무시
    //SceneManager.SetActiveScene을 통해 새로 불러온 씬을 메인으로 설정
    //전역화 시켜서 게임 종료 시까지 컨트롤러가 모든 씬 전환을 책임짐
}