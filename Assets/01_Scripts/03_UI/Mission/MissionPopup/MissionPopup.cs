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

    // =========================
    // UIHardReset 이후에는 Show 요청을 무시하기 위한 플래그
    // - 타이틀 이동 / 강제 종료 중 깜빡임 방지용
    // =========================
    private bool _isGloballyDisabled;

    private void Awake()
    {
        _onShow = OnShowRequested;
        _onUIHardReset = OnUIHardReset;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    private void OnShowRequested(MissionPopupShowRequestedEvent e)
    {
        // =========================
        // 전역 종료 상태(UIHardReset 이후)에서는
        // Show 요청이 와도 무조건 무시
        // =========================
        if (_isGloballyDisabled)
            return;

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
        // =========================
        // 전역 비활성 상태로 전환
        // - 이후 들어오는 Show 이벤트 차단
        // =========================
        _isGloballyDisabled = true;

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






