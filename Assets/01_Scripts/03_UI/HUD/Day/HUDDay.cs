using TMPro;
using UnityEngine;
using System;

public class HUDDay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;   // 배경 포함 전체 패널
    [SerializeField] private TextMeshProUGUI dayText;

    private bool _panelActive;

    private Action<GameContextReadyEvent> _onContextReady;
    private Action<DayChangedEvent> _onDayChanged;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _onContextReady = OnContextReady;
        _onDayChanged = OnDayChanged;
        _onPhaseChanged = OnPhaseChanged;

        // 안전장치
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onContextReady);
        EventBus.Subscribe(_onDayChanged);
        EventBus.Subscribe(_onPhaseChanged);

        // DDOL HUD 대응: 현재 페이즈 즉시 반영
        if (GameManager.Instance != null)
        {
            ApplyPhase(GameManager.Instance.CurrentPhase);
            SyncFromGameManager();
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onContextReady);
        EventBus.Unsubscribe(_onDayChanged);
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    // =========================
    // Phase 기반 패널 제어
    // =========================
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        ApplyPhase(e.Phase);
    }

    private void ApplyPhase(GamePhase phase)
    {
        bool show = phase == GamePhase.Patrol;

        if (_panelActive == show)
            return;

        _panelActive = show;

        if (panelRoot != null)
            panelRoot.SetActive(show);

        if (show)
            SyncFromGameManager(); //게임매니저 상태와 동기화
    }

    // =========================
    // Day 데이터 처리
    // =========================
    private void OnContextReady(GameContextReadyEvent e)
    {
        if (!_panelActive)
            return;

        dayText.text = $"{e.CurrentDay}";
    }

    private void OnDayChanged(DayChangedEvent e)
    {
        if (!_panelActive)
            return;

        dayText.text = $"{e.CurrentDay}";
    }

    private void SyncFromGameManager()
    {
        if (!_panelActive || GameManager.Instance == null)
            return;

        dayText.text = $"{GameManager.Instance.CurrentDay}";
    }
}


