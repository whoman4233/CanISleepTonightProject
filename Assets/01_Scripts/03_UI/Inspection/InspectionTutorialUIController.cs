using System;
using UnityEngine;

public class InspectionTutorialUIController : MonoBehaviour
{
    [Header("Tutorial Texts")]
    [SerializeField] private GameObject tutorialText1;
    [SerializeField] private GameObject tutorialText2;
    [SerializeField] private GameObject tutorialText3;
    [SerializeField] private GameObject tutorialEndText;

    private Action<InspectionRotateStartedEvent> _onRotateStarted;
    private Action<InspectionRotateEndedEvent> _onRotateEnded;

    private void Awake()
    {
        _onRotateStarted = OnRotateStarted;
        _onRotateEnded = OnRotateEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onRotateStarted);
        EventBus.Subscribe(_onRotateEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onRotateStarted);
        EventBus.Unsubscribe(_onRotateEnded);
    }

    private void OnRotateStarted(InspectionRotateStartedEvent e)
    {
        SetRotateMode(true);
    }

    private void OnRotateEnded(InspectionRotateEndedEvent e)
    {
        SetRotateMode(false);
    }

    private void SetRotateMode(bool rotating)
    {
        tutorialText1.SetActive(!rotating);
        tutorialText3.SetActive(!rotating);
        tutorialEndText.SetActive(!rotating);

        tutorialText2.SetActive(rotating);
    }
}
