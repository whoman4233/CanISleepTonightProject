using System;
using UnityEngine;
using UnityEngine.UI;

public class MissionPopup : MonoBehaviour
{
    [Header("Content Root (BG + Panel)")]
    [SerializeField] private GameObject contentRoot;

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
        Show();
    }

    private void Show()
    {
        contentRoot.SetActive(true);
        InputManager.Instance?.SetDialogueActive(true);
    }

    private void OnConfirmClicked()
    {
        contentRoot.SetActive(false);

        InputManager.Instance?.SetDialogueActive(false);

        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission != null)
        {
            EventBus.Publish(new MissionRevealedEvent(mission));
        }

        EventBus.Publish(new MissionBriefingConfirmedEvent());
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }
}





