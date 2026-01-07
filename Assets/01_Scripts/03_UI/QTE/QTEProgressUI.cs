using System;
using UnityEngine;
using UnityEngine.UI;

public class QTEProgressUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private Action<QTEProgressChangedEvent> _onProgressChanged;

    private void Awake()
    {
        _onProgressChanged = OnProgressChanged;
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

        fillImage.fillAmount = Mathf.Clamp01(e.Current / e.Required);
    }
}
