using System;
using UnityEngine;
using UnityEngine.UI;

public class QTEProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backImage;   // 최대치 기준
    [SerializeField] private Image fillImage;   // 현재 진행도

    // EventBus handler (강한 참조)
    private Action<QTEProgressChangedEvent> _onProgressChanged;

    private void Awake()
    {
        _onProgressChanged = OnProgressChanged;

        // 안전 초기화
        if (fillImage != null)
            fillImage.fillAmount = 0f;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onProgressChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onProgressChanged);
    }

    private void OnProgressChanged(QTEProgressChangedEvent e)
    {
        if (fillImage == null || e.Required <= 0f)
            return;

        float ratio = Mathf.Clamp01(e.Current / e.Required);
        fillImage.fillAmount = ratio;

        // 선택적 연출: 거의 다 찼을 때 Back 강조
        if (backImage != null)
        {
            backImage.color = ratio >= 0.9f
                ? new Color(1f, 0.8f, 0.8f, 1f)   // 살짝 강조
                : Color.white;
        }
    }
}

