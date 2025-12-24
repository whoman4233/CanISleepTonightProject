using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    [SerializeField] private Button btnBack;

    private void Awake()
    {
        btnBack.onClick.AddListener(OnClickBack);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnClickBack()
    {
        EventBus.Publish(new HideSettingsPopupEvent());
    }
}
