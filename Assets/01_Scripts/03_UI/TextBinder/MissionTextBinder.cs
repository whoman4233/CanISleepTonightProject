using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class MissionTextBinder : MonoBehaviour
{
    [SerializeField] private MissionTextRole role;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
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
        if (_text == null)
            return;

        var missionManager = DailyMissionManager.Instance;

        if (missionManager == null ||
            missionManager.CurrentMission == null)
        {
            _text.text = string.Empty;
            return;
        }

        _text.text = TextManager.Instance.GetMissionText(
            missionManager.CurrentMission.MissionTextNo,
            role
        );
    }
}


