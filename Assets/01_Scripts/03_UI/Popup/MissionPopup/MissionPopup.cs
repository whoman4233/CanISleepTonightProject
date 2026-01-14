using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPopup : MonoBehaviour
{
    [Header("Content Root (BG + Panel)")]
    [SerializeField] private GameObject contentRoot;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton; 

    private Action<MissionPopupShowRequestedEvent> _onShow;

    private void Awake()
    {
        _onShow = OnShowRequested;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
    }

    private void OnShowRequested(MissionPopupShowRequestedEvent e)
    {
        var mission = DailyMissionManager.Instance?.CurrentMission;

        ShowInternal(mission);
    }

    private void ShowInternal(DailyMissionStrategy mission)
    {
        if (mission == null)
            return;


        titleText.text = mission.title;
        descriptionText.text = mission.description;

        contentRoot.SetActive(true);

        InputManager.Instance?.SetDialogueActive(true);
    }

    // 버튼 클릭 시 호출됨
    private void OnConfirmClicked()
    {
        contentRoot.SetActive(false);

        InputManager.Instance?.SetDialogueActive(false);

        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission != null)
        {
            // HUD / WhiteBoard용
            EventBus.Publish(new MissionRevealedEvent(mission));
        }

        EventBus.Publish(new MissionBriefingConfirmedEvent());
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }
}




