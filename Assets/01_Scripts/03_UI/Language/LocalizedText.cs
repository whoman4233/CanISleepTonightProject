using TMPro;
using UnityEngine;
using System;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string textId;
    [SerializeField] private TextTableType tableType = TextTableType.UI;

    private TMP_Text _text;
    private string _runtimeOverrideId; // 런타임용 ID
    private bool _useRuntimeId;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TextManager.OnLanguageChanged += Refresh;
        TextManager.OnTextDataReady += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        TextManager.OnLanguageChanged -= Refresh;
        TextManager.OnTextDataReady -= Refresh;
    }

    public void SetRuntimeId(string id)
    {
        _runtimeOverrideId = id;
        _useRuntimeId = true;
        if (TextManager.Instance != null)
            Refresh();
    }

    public void ClearRuntimeOverride()
    {
        _useRuntimeId = false;
        Refresh();
    }

    private void Refresh()
    {
        if (_text == null)
            return;

        if (TextManager.Instance == null)
            return;

        string id = _useRuntimeId ? _runtimeOverrideId : textId;

        if (string.IsNullOrEmpty(id))
            return;

        switch (tableType)
        {
            case TextTableType.Dialogue:
                _text.text = TextManager.Instance.GetText(id);
                break;

            case TextTableType.UI:
                _text.text = TextManager.Instance.GetUIText(id);
                break;

            case TextTableType.Prompt:
                _text.text = TextManager.Instance.GetPromptText(id);
                break;
        }
    }

}


