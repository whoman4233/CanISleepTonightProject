using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowController : MonoBehaviour
{
    private Action<RequestStartNewGameEvent> _onRequestNewGame;

    private void Awake()
    {
        _onRequestNewGame = OnStartNewGame;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onRequestNewGame);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onRequestNewGame);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // IntroScene 진입 시 Standby 명시
        var gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.CurrentPhase != GamePhase.NotStarted)
        {
            gm.ChangePhase(GamePhase.NotStarted);
        }
    }

    private void OnStartNewGame(RequestStartNewGameEvent e)
    {
        SceneManager.LoadScene("02_PlayScene", LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "PlayScene")
            return;
        SceneManager.SetActiveScene(scene);

        // IntroScene 명시적 언로드
        var intro = SceneManager.GetSceneByName("01_IntroScene");
        if (intro.isLoaded)
        {
            SceneManager.UnloadSceneAsync(intro);
        }

        // PlayScene 시작 시 Standby로 재진입
        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Standby));

    }
}


