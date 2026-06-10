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
    private Action<MissionRevealedEvent> _onMissionRevealed;
    private Action<UIHardResetEvent> _onUIHardReset;

    private bool _missionRevealed;

    private void Awake()
    {
        _onPhaseChanged = OnPhaseChanged;
        _onMissionRevealed = OnMissionRevealed;
        _onUIHardReset = OnUIHardReset;

        ResetInternalState();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onMissionRevealed);
        EventBus.Subscribe(_onUIHardReset);

        TextManager.OnLanguageChanged += RefreshMissionText;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onMissionRevealed);
        EventBus.Unsubscribe(_onUIHardReset);

        TextManager.OnLanguageChanged -= RefreshMissionText;
    }

    private void OnMissionRevealed(MissionRevealedEvent e)
    {
        if (e.mission == null || missionDescriptionText == null)
            return;

        _missionRevealed = true;

        string text = TextManager.Instance.GetMissionText(
            e.mission.MissionTextNo,
            MissionTextRole.MissionTitle
        );

        missionDescriptionText.text = text;
        missionDescriptionText.gameObject.SetActive(true);
    }


    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Standby)
        {
            ResetInternalState();
            RefreshDay();

            if (!_missionRevealed && missionDescriptionText != null)
                missionDescriptionText.gameObject.SetActive(false);
        }

        if (e.Phase == GamePhase.Patrol && !_missionRevealed)
        {
            var mission = DailyMissionManager.Instance?.CurrentMission;
            if (mission != null)
                EventBus.Publish(new MissionRevealedEvent(mission));
        }
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        ResetInternalState();
    }

    private void ResetInternalState()
    {
        _missionRevealed = false;

        if (missionDescriptionText != null)
            missionDescriptionText.gameObject.SetActive(false);
    }

    private void RefreshDay()
    {
        if (GameManager.Instance == null || dayText == null)
            return;

        int systemDay = GameManager.Instance.CurrentDay;

        // 기획: 0일차부터 표시
        int displayDay = Mathf.Max(0, systemDay);

        dayText.text = $"{displayDay}";
    }

    private void RefreshMissionText()
    {
        if (!_missionRevealed)
            return;

        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission == null)
            return;

        string text = TextManager.Instance.GetMissionText(
            mission.MissionTextNo,
            MissionTextRole.MissionTitle
        );

        missionDescriptionText.text = text;
    }
}










