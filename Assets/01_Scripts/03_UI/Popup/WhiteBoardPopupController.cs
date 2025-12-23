using UnityEngine;

public class WhiteBoardPopupController : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        IsOpen = false;
    }
    private void OnEnable()
    {
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        EventBus.Publish(new GlobalInputLockReleasedEvent());
    }

    public void Show()
    {
        if (IsOpen) return;

        panel.SetActive(true);
        IsOpen = true;
    }

    public void Hide()
    {
        if (!IsOpen) return;

        panel.SetActive(false);
        IsOpen = false;
    }
}
