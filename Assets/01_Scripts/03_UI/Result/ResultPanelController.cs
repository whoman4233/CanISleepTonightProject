using UnityEngine;
using UnityEngine.UI;
using System;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button nextDayButton;
    private bool _settlementReady;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<SettlementCompletedEvent> _onSettlementCompleted;

    private void Awake()
    {
        nextDayButton.interactable = false;
        nextDayButton.onClick.AddListener(OnClickNextDay);

        _onPhaseChanged = OnPhaseChanged;
        _onSettlementCompleted = OnSettlementCompleted;
    }

    private void OnEnable()
    {
        Debug.Log(typeof(GamePhaseChangedEvent).AssemblyQualifiedName);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onSettlementCompleted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onSettlementCompleted);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        Debug.Log($"[ResultPanelController] PhaseChanged: {e.Phase}");
        if (e.Phase == GamePhase.Settlement)
        {
            Open();
        }
        else
        {
            Close();
        }
    }
    private void OnSettlementCompleted(SettlementCompletedEvent e)
    {
        _settlementReady = true;
        nextDayButton.interactable = true;
    }
    private void Open()
    {
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

    private void OnClickNextDay()
    {
        if (!_settlementReady)
            return;

        Close();
        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Standby));
    }

}
