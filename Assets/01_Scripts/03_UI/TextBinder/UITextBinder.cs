using TMPro;
using UnityEngine;

public class UITextBinder : MonoBehaviour
{
    [SerializeField] private string uiTextId;
    [SerializeField] private TMP_Text target;

    private bool _initialized;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        _initialized = false;
    }

    private void Update()
    {
        // TextManager가 나중에 생성되는 구조 대응
        if (_initialized)
            return;

        if (TextManager.Instance == null)
            return;

        Refresh();
        _initialized = true;
    }

    private void Refresh()
    {
        if (target == null)
        {
            Debug.LogError($"[UITextBinder] Target missing on {name}");
            return;
        }

        string text = TextManager.Instance.GetUIText(uiTextId);
        target.text = text;
    }
}
