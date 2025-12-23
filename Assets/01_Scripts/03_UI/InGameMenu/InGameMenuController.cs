using System;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnReturnToTitle;
    [SerializeField] private Button btnOptions;

    private bool isOpen;

    private Action<PauseMenuOpenRequestedEvent> _onOpenRequested;
    private Action<PauseMenuCloseRequestedEvent> _onCloseRequested;

    private void Awake()
    {
        if (menuRoot != null) menuRoot.SetActive(false);

        if (btnResume != null) btnResume.onClick.AddListener(OnClickResume);
        if (btnReturnToTitle != null) btnReturnToTitle.onClick.AddListener(OnClickReturnToTitle);
        if (btnOptions != null) btnOptions.onClick.AddListener(OnClickOptions);

        _onOpenRequested = _ => SetOpen(true);
        _onCloseRequested = _ => SetOpen(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onOpenRequested);
        EventBus.Subscribe(_onCloseRequested);
        SetOpen(false);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onOpenRequested);
        EventBus.Unsubscribe(_onCloseRequested);
    }

    private void OnClickResume()
    {
        SetOpen(false);
    }

    private void OnClickReturnToTitle()
    {
        SetOpen(false);
        EventBus.Publish(new ResumeGameRequestedEvent());
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }

    private void OnClickOptions()
    {
        EventBus.Publish(new ShowSettingsPopupEvent());
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        if (menuRoot != null)
            menuRoot.SetActive(open);

        if (open)
        {
            EventBus.Publish(new PauseGameRequestedEvent());

            // 반드시 UI Lock 요청
            EventBus.Publish(new GlobalInputLockRequestedEvent());

            EventBus.Publish(new PauseMenuOpenedEvent());
        }
        else
        {
            EventBus.Publish(new ResumeGameRequestedEvent());

            // 반드시 UI Lock 해제
            EventBus.Publish(new GlobalInputLockReleasedEvent());

            EventBus.Publish(new PauseMenuClosedEvent());
        }
    }

}








