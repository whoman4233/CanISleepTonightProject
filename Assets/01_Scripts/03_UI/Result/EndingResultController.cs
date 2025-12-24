using System;
using UnityEngine;

public class EndingResultController : MonoBehaviour
{
    [SerializeField] private GameObject happy;
    [SerializeField] private GameObject bad1;
    [SerializeField] private GameObject bad2;
    [SerializeField] private GameObject bad3;

    private Action<EndingConditionMetEvent> _onEnding;

    private void Awake()
    {
        _onEnding = OnEnding;
        HideAll();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onEnding);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onEnding);
    }

    private void OnEnding(EndingConditionMetEvent e)
    {
        HideAll();

        switch (e.EndingType)
        {
            case GameEndingType.HappyEnding1:
                happy.SetActive(true);
                break;
            case GameEndingType.BadEnding1:
                bad1.SetActive(true);
                break;
            case GameEndingType.BadEnding2:
                bad2.SetActive(true);
                break;
            case GameEndingType.BadEnding3:
                bad3.SetActive(true);
                break;
        }

        Time.timeScale = 0f;
        EventBus.Publish(new GlobalInputLockRequestedEvent());
    }

    private void HideAll()
    {
        happy.SetActive(false);
        bad1.SetActive(false);
        bad2.SetActive(false);
        bad3.SetActive(false);
    }
}
