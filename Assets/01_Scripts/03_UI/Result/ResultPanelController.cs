using UnityEngine;
using UnityEngine.UI;
using System;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button nextDayButton;

    private bool _settlementReady;

    // =========================
    // Event handlers (cache)
    // =========================
    private Action<SettlementStartedEvent> _onSettlementStarted;
    private Action<SettlementCompletedEvent> _onSettlementCompleted;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(OnClickNextDay);

        _onSettlementStarted = OnSettlementStarted;
        _onSettlementCompleted = OnSettlementCompleted;
        _onUIHardReset = OnUIHardReset;

        ResetPanel();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onSettlementStarted);
        EventBus.Subscribe(_onSettlementCompleted);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSettlementStarted);
        EventBus.Unsubscribe(_onSettlementCompleted);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    // =========================
    // Settlement lifecycle
    // =========================

    private void OnSettlementStarted(SettlementStartedEvent e)
    {
        Open();
    }

    private void OnSettlementCompleted(SettlementCompletedEvent e)
    {
        Debug.Log("[ResultPanelController] SettlementCompletedEvent RECEIVED");

        _settlementReady = true;

        if (nextDayButton != null)
            nextDayButton.interactable = true;
    }

    // =========================
    // UI Hard Reset (인트로 이동 시)
    // =========================

    private void OnUIHardReset(UIHardResetEvent e)
    {
        Debug.Log("[ResultPanelController] UIHardResetEvent RECEIVED");

        // Result UI는 강제 종료 대상
        ForceClose();
    }

    // =========================
    // UI Control
    // =========================

    private void Open()
    {
        ResetPanel();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        // Result는 플레이를 완전히 막는 상태
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    private void Close()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void ForceClose()
    {
        ResetPanel();

        // 혹시 잠금이 남아 있을 수 있으므로 안전하게 복구
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void ResetPanel()
    {
        _settlementReady = false;

        if (nextDayButton != null)
            nextDayButton.interactable = false;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // =========================
    // Button
    // =========================

    private void OnClickNextDay()
    {
        if (!_settlementReady)
            return;

        Close();

        if (GameManager.Instance != null)
            GameManager.Instance.OnClickSettlementButton();
    }
}


