using System;
using UnityEngine;
using UnityEngine.UI;

public class QTEKeyFeedbackUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image keyImage;
    [SerializeField] private RectTransform keyTransform;

    [Header("Visual")]
    [SerializeField] private Color pressedColor = Color.gray;
    [SerializeField] private Vector3 pressedScale = new Vector3(0.95f, 0.95f, 1f);

    private Color _defaultColor;
    private Vector3 _defaultScale;

    private Action<QTEInputFeedbackEvent> _onInput;

    private void Awake()
    {
        _defaultColor = keyImage.color;
        _defaultScale = keyTransform.localScale;

        _onInput = OnInputFeedback;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onInput);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onInput);
    }

    private void OnInputFeedback(QTEInputFeedbackEvent e)
    {
        if (e.State == QTEInputState.Pressed)
        {
            keyImage.color = pressedColor;
            keyTransform.localScale = pressedScale;
        }
        else
        {
            keyImage.color = _defaultColor;
            keyTransform.localScale = _defaultScale;
        }
    }
}
