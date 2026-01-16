using System;
using UnityEngine;
using DG.Tweening;

public class HUDCellInspectionStatus : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Blink Settings")]
    [SerializeField] private float minAlpha = 0.6f;
    [SerializeField] private float maxAlpha = 1.0f;
    [SerializeField] private float blinkDuration = 0.8f;

    private Tween _blinkTween;
    private bool _active;

    private Action<CellInspectionInProgressEvent> _onStart;
    private Action<CellInspectionCompletedEvent> _onEnd;
    private Action<UIHardResetEvent> _onHardReset;

    private void Awake()
    {
        SetVisible(false);

        _onStart = OnInspectionStart;
        _onEnd = OnInspectionEnd;
        _onHardReset = OnUIHardReset;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onStart);
        EventBus.Subscribe(_onEnd);
        EventBus.Subscribe(_onHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onStart);
        EventBus.Unsubscribe(_onEnd);
        EventBus.Unsubscribe(_onHardReset);
        KillTween();
    }

    private void OnInspectionStart(CellInspectionInProgressEvent e)
    {
        if (_active)
            return;

        _active = true;
        Show();
    }

    private void OnInspectionEnd(CellInspectionCompletedEvent e)
    {
        if (!_active)
            return;

        _active = false;
        Hide();
    }
    private void OnUIHardReset(UIHardResetEvent e)
    {
        _active = false;
        Hide();
    }
    private void Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = minAlpha;

        KillTween();

        _blinkTween = canvasGroup //깜빡 거리는 연출 효과
            .DOFade(maxAlpha, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void Hide()
    {
        KillTween();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? minAlpha : 0f;
        canvasGroup.gameObject.SetActive(visible);
    }

    private void KillTween()
    {
        if (_blinkTween != null)
        {
            _blinkTween.Kill();
            _blinkTween = null;
        }
    }
}
