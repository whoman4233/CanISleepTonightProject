using System;
using UnityEngine;
using UnityEngine.UI;

public class QTETimerUI : MonoBehaviour
{
    [SerializeField] private Image timerFillImage;

    private Action<QTETimerChangedEvent> _onTimerChanged;
    private Action<QTEStartedEvent> _onQTEStarted;

    private void Awake()
    {
        _onTimerChanged = OnTimerChanged;
        _onQTEStarted = OnQTEStarted;

        ResetUI();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onTimerChanged);
        EventBus.Subscribe(_onQTEStarted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onTimerChanged);
        EventBus.Unsubscribe(_onQTEStarted);
    }
    private void OnQTEStarted(QTEStartedEvent e)
    {
        ResetUI();
    }

    private void OnTimerChanged(QTETimerChangedEvent e)
    {
        if (timerFillImage == null || e.Limit <= 0f)
            return;

        timerFillImage.fillAmount = Mathf.Clamp01(e.Remaining / e.Limit);
    }

    private void ResetUI()
    {
        if (timerFillImage != null)
            timerFillImage.fillAmount = 1f;
    }
}
