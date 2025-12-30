using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [Header("Day Text")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Riot Gauge")]
    [SerializeField] private Image gaugeFillBar;              // 폭동게이지 이미지
    [SerializeField] private TextMeshProUGUI gaugeCurrentMaxText; // 폭동게이지 수치 텍스트

    private Action<ResultUIShowRequestedEvent> _onResultUIShow;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _onResultUIShow = OnResultUIShow;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onResultUIShow);
        EventBus.Subscribe(_onPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onResultUIShow);
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    private void OnResultUIShow(ResultUIShowRequestedEvent e)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (GameManager.Instance == null)
            return;

        // -------------------------
        // Day 표시
        // -------------------------
        dayText.text =
            $"Day : {GameManager.Instance.CurrentDay} / {GameManager.Instance.MaxDay}";

        // -------------------------
        // Riot Gauge 계산
        // -------------------------
        int current = GameManager.Instance.CurrentRiotGauge;
        int max = GameManager.Instance.MaxRiotGauge;

        float fill = max > 0 ? (float)current / max : 0f;
        fill = Mathf.Clamp01(fill);

        if (gaugeFillBar != null)
        {
            gaugeFillBar.fillAmount = fill;
        }

        if (gaugeCurrentMaxText != null)
        {
            gaugeCurrentMaxText.text = $"{current} / {max}";
        }
    }


    // =========================
    // Phase 변경
    // =========================
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Standby)
        {
            Refresh(); // Day가 증가한 직후
        }
    }
}





