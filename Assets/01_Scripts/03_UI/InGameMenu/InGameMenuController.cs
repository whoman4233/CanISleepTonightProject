using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnReturnToTitle;
    [SerializeField] private Button btnOptions;

    private bool isOpen;

    private void Awake()
    {
        menuRoot.SetActive(false);
        btnResume.onClick.AddListener(OnClickResume);
        btnReturnToTitle.onClick.AddListener(OnClickReturnToTitle);
        btnOptions.onClick.AddListener(OnClickOptions);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PauseMenuOpenRequestedEvent>(OnOpenRequested);
        EventBus.Subscribe<PauseMenuCloseRequestedEvent>(OnCloseRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PauseMenuOpenRequestedEvent>(OnOpenRequested);
        EventBus.Unsubscribe<PauseMenuCloseRequestedEvent>(OnCloseRequested);

    }

    private void OnOpenRequested(PauseMenuOpenRequestedEvent e)
    {
        SetOpen(true);
    }

    private void OnCloseRequested(PauseMenuCloseRequestedEvent e)
    {
        SetOpen(false);
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
        if (isOpen == open)
            return;

        isOpen = open;
        menuRoot.SetActive(open);

        if (open)
        {
            EventBus.Publish(new PauseGameRequestedEvent());
            EventBus.Publish(new PauseMenuOpenedEvent());
        }
        else
        {
            EventBus.Publish(new ResumeGameRequestedEvent());
            EventBus.Publish(new PauseMenuClosedEvent());
        }
    }
}



