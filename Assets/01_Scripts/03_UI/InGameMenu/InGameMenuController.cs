using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnReturnToTitle;
    [SerializeField] private Button btnOptions;

    private void Awake()
    {
        btnResume.onClick.AddListener(OnClickResume);
        btnReturnToTitle.onClick.AddListener(OnClickReturnToTitle);
        btnOptions.onClick.AddListener(OnClickOptions);
    }

    private void OnEnable()
    {
        ShowCursor();
    }

    private void OnDisable()
    {
        HideCursor();
    }
    private void OnClickResume()
    {
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void OnClickReturnToTitle()
    {
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }

    private void OnClickOptions()
    {
        EventBus.Publish(new ShowSettingsPopupEvent());
    }
    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
