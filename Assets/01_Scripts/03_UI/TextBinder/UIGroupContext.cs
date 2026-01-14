using UnityEngine;

/// <summary>
/// 하나의 UI 패널/프리팹이 어떤 Group(Section)에 속하는지 정의
/// 예: MissionPopup, ResultPopup, PauseMenu, OptionPanel 등
/// </summary>
public class UIGroupContext : MonoBehaviour
{
    [Tooltip("UI Group / Section 이름 (CSV의 key 또는 section 역할)")]
    public string section;
}
