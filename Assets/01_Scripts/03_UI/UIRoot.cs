using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private const string IntroSceneName = "01_IntroScene";
    private const string OutroSceneName = "03_OutroScene";


    // =========================
    // [추가] Editor / Test 설정
    // =========================
    [Header("Test / Editor Settings")]
    [SerializeField] private bool allowTestPhaseInEditor = true;

    // =========================
    // Runtime State
    // =========================
    private GamePhase currentPhase = GamePhase.NotStarted;
    private string currentScene = string.Empty;
    private bool isLoading;

    // =========================
    // Event Handlers (Strong Reference)
    // =========================
    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<SceneChangedEvent> _onSceneChanged;
    private Action<UIHardResetEvent> _onUIHardReset;
    private Action<LoadingOverlayShownEvent> _onLoadingShown;
    private Action<LoadingOverlayHiddenEvent> _onLoadingHidden;

    // =====================================================
    // Lifecycle
    // =====================================================
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 이벤트 핸들러 바인딩 (강한 참조)
        _onPhaseChanged = OnPhaseChanged;
        _onSceneChanged = OnSceneChanged;
        _onUIHardReset = OnUIHardReset;
        _onLoadingShown = OnLoadingOverlayShown;
        _onLoadingHidden = OnLoadingOverlayHidden;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onSceneChanged);
        EventBus.Subscribe(_onUIHardReset);
        EventBus.Subscribe(_onLoadingShown);
        EventBus.Subscribe(_onLoadingHidden);

        // 초기 상태 동기화
        SyncStateFromRuntime();
        RefreshUI();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onSceneChanged);
        EventBus.Unsubscribe(_onUIHardReset);
        EventBus.Unsubscribe(_onLoadingShown);
        EventBus.Unsubscribe(_onLoadingHidden);
    }

    // =====================================================
    // Event Handlers
    // =====================================================

    /// <summary>
    /// GamePhase 변경 시 UI 재계산
    /// </summary>
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        currentPhase = e.Phase;
        RefreshUI();
    }

    /// <summary>
    /// 씬 변경 알림
    /// 실제 UI 판단은 항상 ActiveScene 기준
    /// </summary>
    private void OnSceneChanged(SceneChangedEvent e)
    {
        // =========================
        // Test Phase에서는 Scene 변화 무시
        // =========================
        if (currentPhase == GamePhase.Test)
            return;

        // UI 씬 자체는 무시
        if (e.SceneName == UISceneName)
            return;

        // =====================================================
        // SceneChangedEvent는 트리거 용도
        // 실제 판단은 ActiveScene 기준
        // =====================================================
        currentScene = SceneManager.GetActiveScene().name;
        RefreshUI();
    }

    /// <summary>
    /// UI 강제 리셋
    /// - "끄기"가 아니라 상태 재동기화
    /// </summary>
    private void OnUIHardReset(UIHardResetEvent e)
    {
        SyncStateFromRuntime();
        RefreshUI();
    }

    /// <summary>
    /// 로딩 시작
    /// </summary>
    private void OnLoadingOverlayShown(LoadingOverlayShownEvent e)
    {
        isLoading = true;
        RefreshUI();
    }

    /// <summary>
    /// 로딩 종료
    /// </summary>
    private void OnLoadingOverlayHidden(LoadingOverlayHiddenEvent e)
    {
        isLoading = false;
        SyncStateFromRuntime();
        RefreshUI();
    }

    // =====================================================
    // Internal State Sync
    // =====================================================
    /// <summary>
    /// GameManager / ActiveScene 기준으로
    /// 내부 상태를 강제 동기화
    /// </summary>
    private void SyncStateFromRuntime()
    {
        if (GameManager.Instance != null)
            currentPhase = GameManager.Instance.CurrentPhase;

        currentScene = SceneManager.GetActiveScene().name;
        isLoading = false;
    }

    // =====================================================
    // UI Refresh Core
    // =====================================================
    private void RefreshUI()
    {
        if (currentScene == OutroSceneName)
        {
            foreach (var group in canvasGroups)
            {
                if (group.canvas != null)
                    group.canvas.SetActive(false);
            }
            return;
        }

        // UI 씬이 아직 로드되지 않았으면 무시
        if (!gameObject.scene.isLoaded)
            return;

        // =========================
        // Test Phase 처리
        // =========================
        if (currentPhase == GamePhase.Test)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !allowTestPhaseInEditor)
                return;
#endif
            foreach (var group in canvasGroups)
            {
                if (group.canvas != null)
                    group.canvas.SetActive(true);
            }
            return;
        }

        bool isMenu =
            currentScene == IntroSceneName ||
            currentScene == OutroSceneName ||
            currentPhase == GamePhase.NotStarted;

        bool isTutorial =
            currentPhase == GamePhase.Tutorial;

        foreach (var group in canvasGroups)
        {
            if (group.canvas == null)
                continue;

            bool active;

            // =========================
            // Loading 중 → 전부 비활성
            // =========================
            if (isLoading)
            {
                active = false;
            }
            // =========================
            // Menu / Intro
            // =========================
            else if (isMenu)
            {
                active = group.showInMenu;
            }
            // =========================
            // Tutorial
            // =========================
            else if (isTutorial)
            {
                active = group.showInTutorial;
            }
            // =========================
            // Gameplay (기본)
            // =========================
            else
            {
                active = group.showInGameplay;
            }

            group.canvas.SetActive(active);
        }
    }
}

