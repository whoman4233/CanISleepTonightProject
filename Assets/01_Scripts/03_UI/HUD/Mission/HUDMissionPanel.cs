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

    private Action<MissionStartedEvent> _onStart;
    private Action<MissionProgressChangedEvent> _onProgress;

    private void Awake()
    {
        _onStart = OnMissionStarted;
        _onProgress = OnMissionProgressChanged;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onStart);
        EventBus.Subscribe(_onProgress);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStart);
        EventBus.Unsubscribe(_onProgress);
    }

    // ========================================================================
    // Event Handlers
    // ========================================================================

    private void OnMissionStarted(MissionStartedEvent e)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        titleText.text = e.mission.title;
        descriptionText.text = e.mission.description;

        UpdateProgressText(0, e.mission.targetScore, e.mission);
    }

    private void OnMissionProgressChanged(MissionProgressChangedEvent e)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        var mission = DailyMissionManager.Instance.CurrentMission;
        if (mission == null)
            return;

        UpdateProgressText(e.current, e.target, mission);
    }

    // ========================================================================
    // Progress Text
    // ========================================================================

    private void UpdateProgressText(int current, int target, DailyMissionStrategy mission)
    {
        // Collection 미션
        if (mission is Mission_CollectionStrategy collection)
        {
            // 예: "Weapon 회수 2 / 6"
            progressText.text = $"{collection.targetItemTag} 회수 {current} / {target}";
        }
        // Suppression 미션
        else if (mission is Mission_SuppressionStrategy)
        {
            // 예: "위험 요소 제거 1 / 3"
            progressText.text = $"위험 요소 제거 {current} / {target}";
        }
        // 기본 (확장 대비)
        else
        {
            progressText.text = $"{current} / {target}";
        }
    }
}

