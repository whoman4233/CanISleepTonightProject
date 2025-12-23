using UnityEngine;
using UnityEngine.UI;
using System;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button nextDayButton;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        nextDayButton.onClick.AddListener(OnClickNextDay);

        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Settlement)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    private void Open()
    {
        resultPanel.SetActive(true);
        EventBus.Publish(new GlobalInputLockRequestedEvent());
    }

    private void Close()
    {
        resultPanel.SetActive(false);
        EventBus.Publish(new GlobalInputLockReleasedEvent());
    }

    private void OnClickNextDay()
    {
        Close();

        // 다음 날 흐름 재개
        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Standby));
    }
}
