using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPopupController : MonoBehaviour
{
    [SerializeField] private Button btnYes;
    [SerializeField] private Button btnNo;

    private PopupCanvasRootController root;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        root = GetComponentInParent<PopupCanvasRootController>();

        btnYes.onClick.AddListener(OnClickYes);
        btnNo.onClick.AddListener(OnClickNo);

        IsOpen = false;
    }

    public void Show()
    {
        if (IsOpen) return;

        Debug.Log($"[Show] Target name = {gameObject.name}, instanceID = {gameObject.GetInstanceID()}");
        gameObject.SetActive(true);
        IsOpen = true;
        Debug.Log($"[Show After] activeSelf = {gameObject.activeSelf}");
    }

    public void Hide()
    {
        if (!IsOpen) return;

        gameObject.SetActive(false);
        IsOpen = false;
    }

    private void OnClickYes()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnClickNo()
    {
        root.CloseAllPopups();
    }
}

