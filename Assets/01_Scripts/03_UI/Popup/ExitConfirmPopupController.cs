using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPopupController : MonoBehaviour
{
    [SerializeField] private Button btnYes;
    [SerializeField] private Button btnNo;

    private PopupCanvasRootController root;

    private void Awake()
    {
        root = GetComponentInParent<PopupCanvasRootController>();

        btnYes.onClick.AddListener(OnClickYes);
        btnNo.onClick.AddListener(OnClickNo);

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
