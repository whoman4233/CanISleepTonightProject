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
        EventBus.Publish(new PauseGameRequestedEvent()); //GameManager에 요청(시간 일시정지용)
    }

    private void OnDisable()
    {
        HideCursor();
        EventBus.Publish(new ResumeGameRequestedEvent());
    }
    private void OnClickResume()
    {
        gameObject.SetActive(false);
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
