using UnityEngine;
using UnityEngine.UI;

public class GameOverPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button titleButton;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (titleButton != null)
            titleButton.onClick.AddListener(OnTitleClicked);
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

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
        GameManager.Instance.SetStandbyEnterReason(StandbyEnterReason.RestartSameDay);
        EventBus.Publish(new RequestGameRestartEvent());
    }

    private void OnTitleClicked()
    {
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }
}

