using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class WarningPopupController : MonoBehaviour
{
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private float displayDuration = 1f;

    private Coroutine _currentRoutine;
    private Action<ShowWarningPopupEvent> _onWarning;

    private void Awake()
    {
        warningRoot.SetActive(false);
        _onWarning = OnShowWarning;
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

        _currentRoutine = StartCoroutine(ShowRoutine(e.Message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        warningText.text = message;
        warningRoot.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        warningRoot.SetActive(false);
        _currentRoutine = null;
    }
}
