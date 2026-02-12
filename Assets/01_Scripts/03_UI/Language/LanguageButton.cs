using UnityEngine;
using UnityEngine.UI;

public class LanguageButton : MonoBehaviour
{
    [SerializeField] private Language targetLanguage;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (TextManager.Instance == null)
        {
            Debug.LogError("TextManager Instance 없음");
            return;
        }

        TextManager.Instance.SetLanguage(targetLanguage);
    }
}
