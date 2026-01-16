using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDTimer : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root; // 실제 표시 제어용 Root

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Image timerIcon;

    [Header("Fill Bar")]
    [SerializeField] private Color startColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 1f);

    private bool _isActive;
    private float _currentSeconds;
    private float _lastSeconds;
    private float _initialSeconds;

    private GamePhase _currentPhase; // 현재 Phase 캐시

    private Action<GameContextReadyEvent> _onContextReady;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _onContextReady = OnGameContextReady;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onContextReady);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnInGameTimeUpdated += OnTimeUpdated;
            _currentPhase = GameManager.Instance.CurrentPhase;
        }

        ForceRefreshVisibility(); // 이벤트 기다리지 않고 즉시 동기화
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
    // Event Handling
    // =========================

    private void OnGameContextReady(GameContextReadyEvent e)
    {
        //  Root 직접 제어 금지, 상태만 리셋
        _isActive = false;
        _lastSeconds = 0f;
        _initialSeconds = -1f;

        if (GameManager.Instance != null)
            _currentPhase = GameManager.Instance.CurrentPhase;

        ForceRefreshVisibility();
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        _currentPhase = e.Phase;
        ForceRefreshVisibility();
    }

    // =========================
    // Visibility
    // =========================

    private void ForceRefreshVisibility()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility() // Root 제어는 여기서만
    {
        bool show = _currentPhase == GamePhase.Patrol;

        if (root != null)
            root.SetActive(show);

        if (show)
            SyncFromGameManager();
    }

    // =========================
    // Sync
    // =========================

    private void SyncFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        _currentSeconds = GameManager.Instance.CurrentInGameSeconds;

        if (_initialSeconds <= 0f)
            _initialSeconds = Mathf.Max(0.01f, _currentSeconds);

        UpdateVisuals(_currentSeconds);
    }

    private void OnTimerReset(PatrolTimerResetEvent e)
    {
        _initialSeconds = Mathf.Max(0.01f, e.InitialSeconds);
        _currentSeconds = e.InitialSeconds;
        UpdateVisuals(_currentSeconds);
    }

    private void OnTimeUpdated(float seconds)
    {
        if (_currentPhase != GamePhase.Patrol)
            return;

        _currentSeconds = seconds;
        UpdateVisuals(seconds);
    }

    // =========================
    // UI Update
    // =========================

    private void UpdateVisuals(float seconds)
    {
        UpdateText(seconds);
        UpdateFill(seconds);
    }

    private void UpdateText(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);

        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);

        timerText.text = $"{min:00}:{sec:00}";
    }

    private void UpdateFill(float seconds)
    {
        if (_initialSeconds <= 0f)
            return;

        float normalized = Mathf.Clamp01(seconds / _initialSeconds);
        timerFillImage.fillAmount = normalized;
        timerFillImage.color = Color.Lerp(endColor, startColor, normalized);
    }
}




