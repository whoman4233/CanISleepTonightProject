using System;
using UnityEngine;
using UnityEngine.UI;

public class QTEProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backImage;   // 최대치 기준
    [SerializeField] private Image fillImage;   // 현재 진행도

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 8f;

    private float _currentRatio;   // 실제 표시값
    private float _targetRatio;    // 목표값 (이벤트로 갱신)

    private Action<QTEProgressChangedEvent> _onProgressChanged;
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    private void Awake()
    {
        _onProgressChanged = OnProgressChanged;
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
        ResetUI();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onProgressChanged);
        EventBus.Subscribe(_onQTEStarted);
        EventBus.Subscribe(_onQTEEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onProgressChanged);
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        // 현재 표시값을 목표값으로 부드럽게 이동
        _currentRatio = Mathf.Lerp(
            _currentRatio,
            _targetRatio,
            Time.deltaTime * smoothSpeed
        );

        fillImage.fillAmount = _currentRatio;
    }

    private void OnQTEStarted(QTEStartedEvent e)
    {
        ResetUI();
    }
    private void OnQTEEnded(QTEEndedEvent e)
    {
        ResetUI();
    }

    private void OnProgressChanged(QTEProgressChangedEvent e)
    {
        if (fillImage == null || e.Required <= 0f)
            return;

        _targetRatio = Mathf.Clamp01(e.Current / e.Required);

        // 거의 다 찼을 때 Back 강조
        if (backImage != null)
        {
            backImage.color = _targetRatio >= 0.8f
                ? new Color(1f, 0.8f, 0.8f, 1f)
                : Color.white;
        }
    }

    private void ResetUI()
    {
        _currentRatio = 0f;
        _targetRatio = 0f;

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        if (backImage != null)
            backImage.color = Color.white;
    }
}


