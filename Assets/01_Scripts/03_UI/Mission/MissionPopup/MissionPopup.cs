using System;
using UnityEngine;
using UnityEngine.UI;

public class MissionPopup : MonoBehaviour
{
    [Header("Content Root (BG + Panel)")]
    [SerializeField] private GameObject contentRoot;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    [Header("UISound")]
    [SerializeField] private AudioClip openClip;

    private Action<MissionPopupShowRequestedEvent> _onShow;
    private Action<UIHardResetEvent> _onUIHardReset;
    private Action<GameContextReadyEvent> _onGameContextReady;


    private void Awake()
    {
        _onShow = OnShowRequested;
        _onUIHardReset = OnUIHardReset;


        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onGameContextReady);
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onGameContextReady);
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    private void OnShowRequested(MissionPopupShowRequestedEvent e)
    {
        Show();
    }

    private void Show()
    {
        AudioManager.Instance?.PlayUISound(openClip);
        contentRoot.SetActive(true);

        // MissionPopup은 Dialogue 계열 UI이므로
        // Player 입력 차단 용도로 DialogueActive 사용
        InputManager.Instance?.SetDialogueActive(true);
    }

    private void OnConfirmClicked()
    {
        HideImmediate();

        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission != null)
        {
            EventBus.Publish(new MissionRevealedEvent(mission));
        }

        EventBus.Publish(new MissionBriefingConfirmedEvent());
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    // =========================
    // UI Hard Reset 처리
    // =========================
    private void OnUIHardReset(UIHardResetEvent e)
    {
        // UI 즉시 숨김만 처리
        HideImmediate();
    }

    // =========================
    // 공통 Hide 처리
    // - Confirm / HardReset 양쪽에서 사용
    // - HardReset에서는 절대 게임 흐름 이벤트를 발행하지 않음
    // =========================
    private void HideImmediate()
    {
        contentRoot.SetActive(false);
        InputManager.Instance?.SetDialogueActive(false);
    }

}






