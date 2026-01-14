using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class WarningPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI text;

    private Coroutine _routine;
    private Action<ShowTimedTextPopupEvent> _onShow;

    //Realtime 캐시
    private readonly Dictionary<float, WaitForSecondsRealtime> _waitRealtimeCache =
        new Dictionary<float, WaitForSecondsRealtime>();

    private void Awake()
    {
        if (root != null)
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

        if (text != null)
            text.text = e.Message;

        _routine = StartCoroutine(ShowRoutineRealtime(e.Duration));
    }

    private IEnumerator ShowRoutineRealtime(float duration)
    {
        if (root != null)
            root.SetActive(true);

        yield return GetWaitRealtime(duration); 

        if (root != null)
            root.SetActive(false);

        _routine = null;
    }

    private WaitForSecondsRealtime GetWaitRealtime(float time)
    {
        if (!_waitRealtimeCache.TryGetValue(time, out var wait))
        {
            wait = new WaitForSecondsRealtime(time);
            _waitRealtimeCache.Add(time, wait);
        }
        return wait;
    }
}


