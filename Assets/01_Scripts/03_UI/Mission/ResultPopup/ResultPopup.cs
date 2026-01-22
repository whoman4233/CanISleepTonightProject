using System;
using UnityEngine;

public class ResultPopup : MonoBehaviour
{
    [SerializeField] private GameObject resultContent;
    [SerializeField] private ResultSuccessPanel successPanel;
    [SerializeField] private ResultFailPanel failPanel;

    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;

    private Action<ResultUIShowRequestedEvent> _onShow;
    private Action<UIHardResetEvent> _onUIHardReset;
    private void Awake()
    {
        _onShow = OnResultShowRequested;
        _onUIHardReset = OnUIHardReset;

        HideAll();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    private void OnResultShowRequested(ResultUIShowRequestedEvent e)
    {
        EventBus.Publish(new GlobalInputLockRequestedEvent());

        resultContent.SetActive(true);

        successPanel.Hide();
        failPanel.Hide();

        if (e.isSuccess)
        {
            AudioManager.Instance.PlayUISound(successSound);

            successPanel.Show();

            DailyMissionManager.Instance.ConsumeCurrentMission();
        }
        else
        {
            AudioManager.Instance.PlayUISound(failSound);
            failPanel.Show();
        }
    }
    private void OnUIHardReset(UIHardResetEvent e)
    {
        HideAll();
    }
    public void CloseResultUI()
    {
        HideAll();

        EventBus.Publish(new GlobalInputLockReleasedEvent());
    }

    private void HideAll()
    {
        if (resultContent != null)
            resultContent.SetActive(false);

        if (successPanel != null)
            successPanel.Hide();

        if (failPanel != null)
            failPanel.Hide();
    }
}











