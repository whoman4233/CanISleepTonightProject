using UnityEngine;

public class PopupCanvasRootController : MonoBehaviour
{
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;

    private System.Action<ShowExitConfirmPopupEvent> showExitHandler;

    private void Awake()
    {
        showExitHandler = OnShowExitConfirm;

        exitConfirmPopup.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(showExitHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(showExitHandler);
    }

    private void OnShowExitConfirm(ShowExitConfirmPopupEvent evt)
    {
        Debug.Log("ExitConfirmPopupEvent received");
        exitConfirmPopup.Show();
    }

    public void CloseAllPopups()
    {
        exitConfirmPopup.Hide();
    }
}

