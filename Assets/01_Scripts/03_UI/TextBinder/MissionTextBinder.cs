using System;
using TMPro;
using UnityEngine;

public class MissionTextBinder : MonoBehaviour
{
    [Header("Mission Text Key")]
    [SerializeField] private MissionTextRole role;

    [Header("Target")]
    [SerializeField] private TMP_Text target;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<MissionStartedEvent>(OnMissionStarted);

        TextManager.OnLanguageChanged += Refresh;
        TextManager.OnTextDataReady += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MissionStartedEvent>(OnMissionStarted);

        TextManager.OnLanguageChanged -= Refresh;
        TextManager.OnTextDataReady -= Refresh;
    }

    private void OnMissionStarted(MissionStartedEvent e)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (target == null)
            return;

        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null)
        {
            target.text = string.Empty;
            return;
        }

        target.text = TextManager.Instance.GetMissionText(
            missionManager.CurrentMission.MissionTextNo,
            role
        );
    }
}

