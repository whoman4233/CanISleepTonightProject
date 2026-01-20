using System;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button titleButton;

    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (titleButton != null)
            titleButton.onClick.AddListener(OnTitleClicked);

        _onUIHardReset = e => Hide();
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onUIHardReset);
    }
    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        Time.timeScale = 0f;

        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnRestartClicked()
    {
        Hide();

        Time.timeScale = 1f;

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());

        // 새 게임(튜토리얼 스킵) 재시작
        EventBus.Publish(new RequestRestartFromFailureEvent());
    }

    private void OnTitleClicked()
    {
        Hide();

        Time.timeScale = 1f;

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());

        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }
}

