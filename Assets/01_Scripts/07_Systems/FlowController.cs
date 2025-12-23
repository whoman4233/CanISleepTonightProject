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

    private bool isBusy = false;

    private Action<RequestStartNewGameEvent> _startNewGameHandler;
    private Action<ReturnToTitleRequestedEvent> _returnToTitleHandler;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _startNewGameHandler = e => StartNewGame();
            _returnToTitleHandler = e => ReturnToTitle();
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
    }
    private void OnDisable()
    {

        EventBus.Unsubscribe(_startNewGameHandler);
        EventBus.Unsubscribe(_returnToTitleHandler);
    }

    public void StartNewGame()
    {
        if (isBusy) return;
        StartCoroutine(LoadPlaySceneSequence());
    }

    private IEnumerator LoadPlaySceneSequence()
    {
        isBusy = true; 

        // 플레이 씬 비동기 로딩
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) yield return null;

        // 신규 씬 활성화
        Scene playScene = SceneManager.GetSceneByName(playSceneName);
        if (playScene.IsValid()) SceneManager.SetActiveScene(playScene);

        // 인트로 씬 언로드
        Scene introScene = SceneManager.GetSceneByName(introSceneName);
        if (introScene.isLoaded) yield return SceneManager.UnloadSceneAsync(introScene);

        // 페이즈 변경
        GameManager.Instance.ChangePhase(GamePhase.Standby);

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

        // 1. 현재 로드된 'PlayScene'만 비동기로 언로드합니다.
        Scene playScene = SceneManager.GetSceneByName(playSceneName);
        if (playScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(playScene);
        }

        // 2. 'IntroScene'을 Additive로 로드합니다. (Single이 아님!)
        if (!SceneManager.GetSceneByName(introSceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(introSceneName, LoadSceneMode.Additive);
        }

        // 3. 인트로 씬을 메인으로 설정
        Scene intro = SceneManager.GetSceneByName(introSceneName);
        SceneManager.SetActiveScene(intro);

        // 4. 상태 초기화
        GameManager.Instance.ChangePhase(GamePhase.NotStarted);

        isBusy = false;
    }



    //LoadSceneAsync = 씬이 로딩되는 동안에도 백그라운드에서 다른 연산(로딩 바 갱신, 팁 출력 등)가능, yield return null을 통해 로딩이 완전히 완료될 때까지 안전하게 기다린 후 다음 코드를 실행.
    //isBusy = 로딩이 진행 중일 때는 추가적인 로딩 요청을 무시
    //SceneManager.SetActiveScene을 통해 새로 불러온 씬을 메인으로 설정
    //전역화 시켜서 게임 종료 시까지 컨트롤러가 모든 씬 전환을 책임짐
}