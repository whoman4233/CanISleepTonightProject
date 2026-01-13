using System;
using TMPro;
using UnityEngine;

public class MissionPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Action<MissionPopupShowRequestedEvent> _onShow;

    private void Awake()
    {
        root.SetActive(false);
        _onShow = OnShowRequested;
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
        ShowInternal(e.mission);
    }

    private void ShowInternal(DailyMissionStrategy mission)
    {
        if (mission == null) return;

        titleText.text = mission.title;
        descriptionText.text = mission.description;

        root.SetActive(true);

        // 입력 차단
        InputManager.Instance?.SetDialogueActive(true);
    }

    public void OnConfirmClicked()
    {
        root.SetActive(false);

        InputManager.Instance?.SetDialogueActive(false);

        // 브리핑 확인 완료 알림
        EventBus.Publish(new MissionBriefingConfirmedEvent());
    }
}


