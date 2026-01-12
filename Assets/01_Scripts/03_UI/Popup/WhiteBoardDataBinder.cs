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

    // 미션 UI 노출 여부 (핵심 상태)
    private bool _missionRevealed;

    private void Awake()
    {
        _onPhaseChanged = OnPhaseChanged;
        _onMissionStarted = OnMissionStarted;

        // 초기 상태: 미션 숨김
        _missionRevealed = false;

        if (missionDescriptionText != null)
            missionDescriptionText.gameObject.SetActive(false);
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
    // Mission 시작 (NPC 상호작용 후)
    // =========================
    private void OnMissionStarted(MissionStartedEvent e)
    {
        if (missionDescriptionText == null)
            return;

        _missionRevealed = true;

        missionDescriptionText.text = e.mission.description;
        missionDescriptionText.gameObject.SetActive(true);
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

            // Standby 진입 시에도 미션은 노출하지 않음
            if (!_missionRevealed && missionDescriptionText != null)
            {
                missionDescriptionText.gameObject.SetActive(false);
            }
        }
    }
}








