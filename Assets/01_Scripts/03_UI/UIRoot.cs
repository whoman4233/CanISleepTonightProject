using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UICanvasGroup
{
    public GameObject canvas;
    public bool showInGameplay;
    public bool showInTutorial;
    public bool showInMenu;
}

public class UIRoot : MonoBehaviour
{
    private static UIRoot instance;

    [Header("Canvas Groups")]
    [SerializeField] private List<UICanvasGroup> canvasGroups;

    // =========================
    // Scene name constants
    // =========================
    private const string UISceneName = "04_UIScene";
    private const string LoadingSceneName = "07_LoadingScene_LSG";
    private const string IntroSceneName = "01_IntroScene";

    private GamePhase currentPhase = GamePhase.NotStarted;
    private string currentScene = string.Empty;
    private bool isLoading;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        currentPhase = e.Phase;
        RefreshUI();
    }

    private void OnSceneChanged(SceneChangedEvent e)
    {
        // UI 씬은 무시
        if (e.SceneName == UISceneName)
            return;

        // Loading 진입
        if (e.SceneName == LoadingSceneName)
        {
            isLoading = true;
            RefreshUI();
            return;
        }

        // Loading이 아닌 씬이 들어오면 로딩 종료
        isLoading = false;

        currentScene = e.SceneName;
        RefreshUI();
    }


    private void RefreshUI()
    {

        bool isMenu = currentScene == IntroSceneName || currentPhase == GamePhase.NotStarted;
        bool isTutorial = currentPhase == GamePhase.Tutorial;

        foreach (var group in canvasGroups)
        {
            if (group.canvas == null)
                continue;

            // =========================
            // Menu / Intro
            // =========================
            if (isMenu)
            {
                group.canvas.SetActive(group.showInMenu);
                continue;
            }

            // =========================
            // Tutorial
            // =========================
            if (isTutorial)
            {
                group.canvas.SetActive(group.showInTutorial);
                continue;
            }

            // =========================
            // Gameplay (기본)
            // =========================
            group.canvas.SetActive(group.showInGameplay);
        }
    }
}

