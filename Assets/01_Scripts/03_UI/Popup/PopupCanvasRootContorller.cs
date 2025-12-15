using UnityEngine;

public class PopupCanvasRootController : MonoBehaviour
{
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;

    private System.Action<ShowExitConfirmPopupEvent> showExitHandler;

    private void Awake()
    {
        showExitHandler = OnShowExitConfirm;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(showExitHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(showExitHandler);
    }

    private void OnShowExitConfirm(ShowExitConfirmPopupEvent e)
    {
        exitConfirmPopup.Show();
        Debug.Log($"[Root] ref name = {exitConfirmPopup.gameObject.name}, instanceID = {exitConfirmPopup.gameObject.GetInstanceID()}");

    }

    public void CloseAllPopups()
    {
        exitConfirmPopup.Hide();
    }
}

