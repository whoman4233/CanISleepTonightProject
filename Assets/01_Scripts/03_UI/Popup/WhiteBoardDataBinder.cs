using System;
using TMPro;
using UnityEngine;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI floor1Text;
    [SerializeField] private TextMeshProUGUI floor2Text;

    private SettlementUIData _cachedData;
    private Action<SettlementUIDataCreatedEvent> _onData;

    private void Awake()
    {
        _onData = OnDataCreated;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onData);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onData);
    }

    private void OnDataCreated(SettlementUIDataCreatedEvent e)
    {
        _cachedData = e.Data;
        Refresh();
    }

    private void Refresh()
    {
        floor1Text.text = _cachedData.Floor1_AnomalyCount.ToString();
        floor2Text.text = _cachedData.Floor2_AnomalyCount.ToString();
    }
}
