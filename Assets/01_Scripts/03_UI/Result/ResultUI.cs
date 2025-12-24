using System;
using UnityEngine;
using TMPro;

[Serializable]
public struct SettlementResultUIData
{
    public int TotalAnomalyCount;

    public int SuppressedCount;
    public int WarnedCount;
    public int UncheckedCount;

    public int RiotGaugeBefore;   
    public int RiotGaugeAfter;    
}
public class ResultUI : MonoBehaviour
{
    [Header("Counts")]
    [SerializeField] private TextMeshProUGUI totalAnomalyText;
    [SerializeField] private TextMeshProUGUI suppressedText;
    [SerializeField] private TextMeshProUGUI warnedText;
    [SerializeField] private TextMeshProUGUI uncheckedText;

    [Header("Riot Gauge")]
    [SerializeField] private TextMeshProUGUI riotGaugeDeltaText;

    private Action<ResultUIShowRequestedEvent> _onShow;

    private void Awake()
    {
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

    private void OnShowRequested(ResultUIShowRequestedEvent e)
    {
        Bind(e.Data);
    }

    public void Bind(SettlementResultUIData data)
    {
        totalAnomalyText.text = data.TotalAnomalyCount.ToString();
        suppressedText.text = data.SuppressedCount.ToString();
        warnedText.text = data.WarnedCount.ToString();
        uncheckedText.text = data.UncheckedCount.ToString();

        int delta = data.RiotGaugeAfter - data.RiotGaugeBefore;

        riotGaugeDeltaText.text =
            $"{data.RiotGaugeBefore} → {data.RiotGaugeAfter} " +
            $"({(delta >= 0 ? "+" : "")}{delta})";
    }
}
