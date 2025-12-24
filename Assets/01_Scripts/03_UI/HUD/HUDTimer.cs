using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Format")]
    [SerializeField] private bool showMilliseconds = true;

    private bool _isActive;
    private float _currentSeconds;

    // =========================
    // 라이프 타임
    // =========================

    private void Awake()
    {
        // Awake에서는 상태만 초기화
        _isActive = false;
        _currentSeconds = 0f;
    }

    private void OnEnable()
    {
        // UI 참조 방어
        if (timerText == null)
        {
            Debug.LogError("[HUDTimer] timerText is not assigned.");
            enabled = false;
            return;
        }

        timerText.gameObject.SetActive(false);

        EventBus.Subscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
            GameManager.Instance.OnInGameTimeUpdated += OnTimeUpdated;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PatrolTimerResetEvent>(OnTimerReset);

        if (GameManager.Instance != null)
            GameManager.Instance.OnInGameTimeUpdated -= OnTimeUpdated;
    }

    // =========================
    // Event handlers
    // =========================

    /// <summary>
    /// Patrol 시작 시 1회 호출
    /// </summary>
    private void OnTimerReset(PatrolTimerResetEvent e)
    {
        _isActive = true;
        _currentSeconds = e.InitialSeconds;

        timerText.gameObject.SetActive(true);
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

    // =========================
    // 타이머 표시
    // =========================

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

    // =========================
    // 비활성화
    // =========================

    public void Deactivate()
    {
        _isActive = false;

        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
}



