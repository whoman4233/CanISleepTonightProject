using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Fill Bar")]
    [SerializeField] private Image timerFillImage;   // Image Type = Filled
    [SerializeField] private Color startColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 1f);

    [Header("Icon")]
    [SerializeField] private Image timerIcon;

    private bool _isActive;
    private float _currentSeconds;
    private float _lastSeconds;
    private float _initialSeconds;

    private Action<GameContextReadyEvent> _onContextReady;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _isActive = false;
        _currentSeconds = 0f;
        _initialSeconds = -1f; // 아직 기준값 없음 표시

        _onContextReady = OnGameContextReady;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        if (timerText == null)
        {
            enabled = false;
            return;
        }

        SetUIActive(false);

        EventBus.Subscribe(_onContextReady);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
            GameManager.Instance.OnInGameTimeUpdated += OnTimeUpdated;

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
    // Context
    // =========================

    private void OnGameContextReady(GameContextReadyEvent e)
    {
        Deactivate();
    }

    private void ApplyPhaseFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        ApplyPhase(GameManager.Instance.CurrentPhase);
    }

    private void ApplyPhase(GamePhase phase)
    {
        if (phase == GamePhase.Patrol)
            Activate();
        else
            Deactivate();
    }

    private void Activate()
    {
        if (_isActive)
            return;

        _isActive = true;
        SetUIActive(true);
        SyncFromGameManager();
    }
    private void Deactivate()
    {
        _isActive = false;
        _lastSeconds = 0f;
        _initialSeconds = -1f; // 다음 Patrol 대비
        SetUIActive(false);
    }

    private void SetUIActive(bool active)
    {
        if (timerText != null)
            timerText.gameObject.SetActive(active);

        if (timerFillImage != null)
            timerFillImage.gameObject.SetActive(active);

        if (timerIcon != null)
            timerIcon.gameObject.SetActive(active);
    }

    // =========================
    // Sync
    // =========================

    private void SyncFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.Patrol)
            return;

        _currentSeconds = GameManager.Instance.CurrentInGameSeconds;

        // Reset 이벤트 이전 진입 대비
        if (_initialSeconds <= 0f)
            _initialSeconds = Mathf.Max(0.01f, _currentSeconds);

        UpdateVisuals(_currentSeconds);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Patrol)
            Activate();
        else
            Deactivate();
    }

    private void OnTimerReset(PatrolTimerResetEvent e)
    {
        _initialSeconds = Mathf.Max(0.01f, e.InitialSeconds);
        _currentSeconds = e.InitialSeconds;

        if (_isActive)
            UpdateVisuals(_currentSeconds);
    }

    private void OnTimeUpdated(float seconds)
    {
        if (!_isActive)
            return;

        if (_lastSeconds > 0f)
        {
            float delta = Mathf.Abs(seconds - _lastSeconds);

            // 정상적인 감소(Time.deltaTime)보다 훨씬 큰 변화면
            if (delta > 1.0f)
            {
                // 기준값 재설정
                _initialSeconds = Mathf.Max(0.01f, seconds);
            }
        }

        _lastSeconds = seconds;
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
        if (timerFillImage == null)
            return;

        if (_initialSeconds <= 0f)
            return;

        float normalized = Mathf.Clamp01(seconds / _initialSeconds);

        timerFillImage.fillAmount = normalized;
        timerFillImage.color = Color.Lerp(endColor, startColor, normalized);
    }
}





