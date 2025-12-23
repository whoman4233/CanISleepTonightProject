using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    [SerializeField] private Button btnBack;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        btnBack.onClick.AddListener(OnClickBack);
        IsOpen = false;
    }

    public void Show()
    {
        if (IsOpen) return;

        gameObject.SetActive(true);
        IsOpen = true;
    }

    public void Hide()
    {
        if (!IsOpen) return;

        gameObject.SetActive(false);
        IsOpen = false;
    }

    private void OnClickBack()
    {
        EventBus.Publish(new HideSettingsPopupEvent());
    }
}

