using System;
using UnityEngine;
using UnityEngine.UI;

public class QTETimerUI : MonoBehaviour
{
    [SerializeField] private Image timerFillImage;

    private Action<QTETimerChangedEvent> _onTimerChanged;

    private void Awake()
    {
        _onTimerChanged = OnTimerChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onTimerChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onTimerChanged);
    }

    private void OnTimerChanged(QTETimerChangedEvent e)
    {
        if (timerFillImage == null || e.Limit <= 0f)
            return;

        timerFillImage.fillAmount = Mathf.Clamp01(e.Remaining / e.Limit);
    }
}
