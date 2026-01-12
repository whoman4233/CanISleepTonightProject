using System;
using TMPro;
using UnityEngine;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [Header("Day Text")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Mission Text")]
    [SerializeField] private TextMeshProUGUI missionDescriptionText;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<MissionStartedEvent> _onMissionStarted;

    private void Awake()
    {
        _onPhaseChanged = OnPhaseChanged;
        _onMissionStarted = OnMissionStarted;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onMissionStarted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onMissionStarted);
    }

    // =========================
    // Mission 시작 (UI 노출 시점)
    // =========================
    private void OnMissionStarted(MissionStartedEvent e)
    {
        if (missionDescriptionText == null)
            return;

        missionDescriptionText.text = e.mission.description;
    }

    // =========================
    // Day 갱신
    // =========================
    private void RefreshDay()
    {
        if (GameManager.Instance == null || dayText == null)
            return;

        dayText.text =
            $"{GameManager.Instance.CurrentDay}";
    }

    // =========================
    // Phase 변경
    // =========================
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Standby)
        {
            RefreshDay();
        }
    }
}







