using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class UITextBinder : MonoBehaviour
{
    [Header("UI Text Binding")]
    [SerializeField] private string role; // Title / Desc / Button / Tooltip

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();

        // role을 비워두면 GameObject 이름을 자동 사용
        if (string.IsNullOrEmpty(role))
            role = gameObject.name;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (TextManager.Instance == null)
            return;

        var screen = GetComponentInParent<UIScreenContext>()?.screen;
        var section = GetComponentInParent<UIGroupContext>()?.section;

        string missionId = DailyMissionManager.Instance?.CurrentMission?.missionId
            ?? "MissionCommon";

        _text.text = TextManager.Instance.GetUIText(
            missionId,
            screen,
            section,
            role
        );
    }
}

