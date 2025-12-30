using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class WarningPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI text;

    private Coroutine _routine;
    private Action<ShowTimedTextPopupEvent> _onShow;

    private void Awake()
    {
        root.SetActive(false);
        _onShow = OnShow;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
    }

    private void OnShow(ShowTimedTextPopupEvent e)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        text.text = e.Message;
        _routine = StartCoroutine(ShowRoutine(e.Duration));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        root.SetActive(true);
        yield return new WaitForSeconds(duration);
        root.SetActive(false);
        _routine = null;
    }
}

