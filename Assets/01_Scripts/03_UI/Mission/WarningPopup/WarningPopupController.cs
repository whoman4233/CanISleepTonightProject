using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class WarningPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private LocalizedText localizedText;

    [Header("UISound")]
    [SerializeField] private AudioClip warningClip;

    private Coroutine _routine;
    private Action<ShowTimedTextPopupEvent> _onShow;
    private Action<UIHardResetEvent> _onHardReset;
    //Realtime 캐시
    private readonly Dictionary<float, WaitForSecondsRealtime> _waitRealtimeCache =
        new Dictionary<float, WaitForSecondsRealtime>();

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        _onShow = OnShow;
        _onHardReset = OnHardReset;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onHardReset);
    }

    private void OnShow(ShowTimedTextPopupEvent e)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        if (localizedText != null)
        {
            localizedText.SetRuntimeId(e.MessageId);
        }

        if (e.PlayBeep && warningClip != null)
        {
            AudioManager.Instance.PlayUISound(warningClip);
        }

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

    private void OnHardReset(UIHardResetEvent e)
    {
        // 진행 중인 타이머 중단
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        // 팝업 즉시 종료
        if (root != null)
            root.SetActive(false);
    }
}


