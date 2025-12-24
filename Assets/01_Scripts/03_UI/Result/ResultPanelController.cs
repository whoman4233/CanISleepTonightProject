using UnityEngine;
using UnityEngine.UI;
using System;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button nextDayButton;

    private bool _settlementReady;

    private Action<SettlementStartedEvent> _onSettlementStarted;
    private Action<SettlementCompletedEvent> _onSettlementCompleted;

    private void Awake()
    {
        nextDayButton.onClick.AddListener(OnClickNextDay);

        _onSettlementStarted = OnSettlementStarted;
        _onSettlementCompleted = OnSettlementCompleted;

        ResetPanel();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onSettlementStarted);
        EventBus.Subscribe(_onSettlementCompleted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSettlementStarted);
        EventBus.Unsubscribe(_onSettlementCompleted);
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
        nextDayButton.interactable = true;
    }

    // =========================
    // UI 
    // =========================

    private void Open()
    {
        ResetPanel();

        resultPanel.SetActive(true);

        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    private void Close()
    {
        resultPanel.SetActive(false);

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void ResetPanel()
    {
        _settlementReady = false;
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

        GameManager.Instance.OnClickSettlementButton();
    }
}

