using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextKeyBinder : MonoBehaviour
{
    [Header("Text Key")]
    [SerializeField] private string textKey;

    private TextMeshProUGUI _text;
    private bool _initialized;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // TextManager가 늦게 생성되는 경우를 대비해
        // 두 이벤트 모두 구독
        TextManager.OnTextDataReady += Apply;
        TextManager.OnLanguageChanged += Apply;

        // 이미 준비돼 있다면 즉시 반영
        Apply();
    }

    private void OnDisable()
    {
        TextManager.OnTextDataReady -= Apply;
        TextManager.OnLanguageChanged -= Apply;
    }

    public void Apply()
    {
        if (_text == null)
            return;

        if (string.IsNullOrEmpty(textKey))
            return;

        if (TextManager.Instance == null)
            return;

        _text.text = TextManager.Instance.GetText(textKey);
        _initialized = true;
    }

    /// <summary>
    /// 런타임 중 TextKey 변경용
    /// (미션 UI, 튜토리얼, 동적 문구 등)
    /// </summary>
    public void SetKey(string key)
    {
        textKey = key;

        if (_initialized)
            Apply();
    }
}

