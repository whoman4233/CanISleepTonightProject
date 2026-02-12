using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string textId;
    [SerializeField] private TextTableType tableType;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TextManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        TextManager.OnLanguageChanged -= Refresh;
    }

    public void SetRuntimeId(string id)
    {
        textId = id;
        Refresh();
    }

    private void Refresh()
    {
        if (string.IsNullOrEmpty(textId))
            return;

        switch (tableType)
        {
            case TextTableType.Dialogue:
                _text.text = TextManager.Instance.GetText(textId);
                break;

            case TextTableType.UI:
                _text.text = TextManager.Instance.GetUIText(textId);
                break;

            case TextTableType.Prompt:
                _text.text = TextManager.Instance.GetPromptText(textId);
                break;
        }
    }
}

