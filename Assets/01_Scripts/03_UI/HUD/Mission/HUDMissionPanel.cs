using UnityEngine;
using TMPro;
using System;

public class HUDMissionPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;

    private Action<MissionRevealedEvent> _onMissionRevealed;
    private Action<MissionProgressChangedEvent> _onProgress;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onMissionRevealed = OnMissionRevealed;
        _onProgress = OnMissionProgressChanged;
        _onUIHardReset = OnUIHardReset;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onMissionRevealed);
        EventBus.Subscribe(_onProgress);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onMissionRevealed);
        EventBus.Unsubscribe(_onProgress);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    private void OnMissionRevealed(MissionRevealedEvent e)
    {
        if (e.mission == null || panelRoot == null)
            return;

        panelRoot.SetActive(true);

        titleText.text = e.mission.title;
        descriptionText.text = e.mission.description;

        UpdateProgressText(0, e.mission.targetScore, e.mission);
    }

    private void OnMissionProgressChanged(MissionProgressChangedEvent e)
    {
        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission == null)
            return;

        UpdateProgressText(e.current, e.target, mission);
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void UpdateProgressText(int current, int target, DailyMissionStrategy mission)
    {
        if (progressText == null)
            return;

        if (mission is Mission_CollectionStrategy collection)
            progressText.text = $"{collection.targetItemTag} 회수 {current} / {target}";
        else if (mission is Mission_SuppressionStrategy)
            progressText.text = $"위험 요소 제거 {current} / {target}";
        else
            progressText.text = $"{current} / {target}";
    }
}




