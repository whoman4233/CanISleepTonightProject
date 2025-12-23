using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class WarningPopupController : MonoBehaviour
{
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float displayDuration = 1f;

    private Coroutine _currentRoutine;
    private Action<ShowWarningPopupEvent> _onWarning;

    private void Awake()
    {
        _onWarning = OnShowWarning;
        warningRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onWarning);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onWarning);
    }

    private void OnShowWarning(ShowWarningPopupEvent e)
    {
        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);

        _currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        warningRoot.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        warningRoot.SetActive(false);
        _currentRoutine = null;
    }
}

