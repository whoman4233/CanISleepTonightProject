using TMPro;
using UnityEngine;
using System;

public class HUDMissionPanel : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Goals")]
    [SerializeField] private Transform goalRoot;
    [SerializeField] private MissionGoalCheckUI goalPrefab;

    private MissionGoalCheckUI[] goalUIs;

    private Action<MissionStartedEvent> _onStart;
    private Action<MissionProgressChangedEvent> _onProgress;

    private void Awake()
    {
        _onStart = OnMissionStarted;
        _onProgress = OnProgressChanged;
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

    private void OnMissionStarted(MissionStartedEvent e)
    {
        titleText.text = e.mission.title;
        descriptionText.text = e.mission.description;

        BuildGoals(e.mission.targetScore);
    }

    private void BuildGoals(int target)
    {
        foreach (Transform child in goalRoot)
            Destroy(child.gameObject);

        goalUIs = new MissionGoalCheckUI[target];

        for (int i = 0; i < target; i++)
        {
            var ui = Instantiate(goalPrefab, goalRoot);
            ui.SetChecked(false);
            goalUIs[i] = ui;
        }
    }

    private void OnProgressChanged(MissionProgressChangedEvent e)
    {
        for (int i = 0; i < goalUIs.Length; i++)
        {
            goalUIs[i].SetChecked(i < e.current);
        }
    }
}
