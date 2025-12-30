using TMPro;
using UnityEngine;
using System;

public class HUDTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Format")]
    [SerializeField] private bool showMilliseconds = true;

    private bool _isActive;
    private float _currentSeconds;
    
    private Action<GameContextReadyEvent> _onContextReady;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _isActive = false;
        _currentSeconds = 0f;

        _onContextReady = OnGameContextReady;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        if (timerText == null)
        {
            Debug.LogError("[HUDTimer] timerText is not assigned.");
            enabled = false;
            return;
        }

        timerText.gameObject.SetActive(false);

        // 컨텍스트 준비 이벤트 구독
        EventBus.Subscribe(_onContextReady);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
            GameManager.Instance.OnInGameTimeUpdated += OnTimeUpdated;
        // DDOL UI는 이미 Phase가 정해진 상태로 들어올 수 있으므로 즉시 재적용
        ApplyPhaseFromGameManager();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onContextReady);
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
            GameManager.Instance.OnInGameTimeUpdated -= OnTimeUpdated;
    }

    // =========================
    // Context Ready
    // =========================
    private void OnGameContextReady(GameContextReadyEvent e)
    {
        // 씬 재로딩/루프 변경의 기준점
        Deactivate();          // 이전 루프 상태 제거
    }
    private void ApplyPhaseFromGameManager() //게임 매니저와 페이즈 동기화
    {
        if (GameManager.Instance == null)
            return;

        ApplyPhase(GameManager.Instance.CurrentPhase);
    }
    private void ApplyPhase(GamePhase phase)
    {
        if (phase == GamePhase.Patrol)
        {
            Activate(); // Patrol이면 켠다
        }
        else
        {
            Deactivate(); // 그 외는 끈다 (원하는 정책에 맞게 조정 가능)
        }
    }
    private void Activate()
    {
        _isActive = true;

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        SyncFromGameManager(); // 값만 복구
    }

    /// <summary>
    /// GameManager에서 현재 상태를 Pull해 UI 복구
    /// </summary>
    private void SyncFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.Patrol)
            return;

        _isActive = true;
        _currentSeconds = GameManager.Instance.CurrentInGameSeconds;

        timerText.gameObject.SetActive(true);
        UpdateText(_currentSeconds);
    }
    /// <summary>
    /// Patrol 시작 시 1회 호출
    /// </summary>
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Patrol)
        {
            SyncFromGameManager();
        }
        else
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Patrol 시작 시 1회 호출
    /// </summary>
    private void OnTimerReset(PatrolTimerResetEvent e)
    {
        _currentSeconds = e.InitialSeconds;

        if (_isActive)
            UpdateText(_currentSeconds);
    }

    /// <summary>
    /// Patrol 중 시간 업데이트
    /// </summary>
    private void OnTimeUpdated(float seconds)
    {
        if (!_isActive)
            return;

        _currentSeconds = seconds;
        UpdateText(_currentSeconds);
    }

    private void UpdateText(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);

        if (showMilliseconds)
        {
            int ms = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
            timerText.text = $"{min:00}:{sec:00}.{ms:00}";
        }
        else
        {
            timerText.text = $"{min:00}:{sec:00}";
        }
    }

    public void Deactivate()
    {
        _isActive = false;

        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
}



