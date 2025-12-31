using System;
using UnityEngine;
using TMPro;

[Serializable]
public struct SettlementResultUIData
{
    public int ReportCurrentDay;
    public int TotalCheckCount;

    public int SuppressedCount;
    public int WarnedCount;
    public int UncheckedCount;

    public int RiotGaugeBefore;   
    public int RiotGaugeAfter;    
}
public class ResultUI : MonoBehaviour
{
    [Header("Counts")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI totalCheckText;
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
        dayText.text = data.ReportCurrentDay.ToString($"{data.ReportCurrentDay}일차 업무 결과");
        totalCheckText.text = data.TotalCheckCount.ToString();
        suppressedText.text = data.SuppressedCount.ToString();
        warnedText.text = data.WarnedCount.ToString();
        uncheckedText.text = data.UncheckedCount.ToString();

        int delta = data.RiotGaugeAfter - data.RiotGaugeBefore;

        riotGaugeDeltaText.text =
            $"{data.RiotGaugeBefore} → {data.RiotGaugeAfter} " +
            $"({(delta >= 0 ? "+" : "")}{delta})";
    }
}
