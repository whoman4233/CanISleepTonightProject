using System;
using UnityEngine;
using TMPro;

public class EndingDayTextBinder : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private string format = "{0}일";

    private Action<EndingUIShowRequestedEvent> _onShow;

    private void Awake()
    {
        if (dayText == null)
            dayText = GetComponent<TextMeshProUGUI>();

        _onShow = OnEndingShowRequested;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
    }

    private void OnEndingShowRequested(EndingUIShowRequestedEvent e)
    {
        Apply(e.Data.WorkingDay);
    }

    private void Apply(int workingDay)
    {
        dayText.text = string.Format(format, workingDay);
    }
}

