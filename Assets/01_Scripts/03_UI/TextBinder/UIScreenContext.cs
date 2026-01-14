using UnityEngine;

/// <summary>
/// 이 Canvas(UI 화면)가 어떤 Screen 타입인지 정의하는 컨텍스트
/// 예: HUD, Popup, InGameMenu, Mission, Inspection 등
/// </summary>
public class UIScreenContext : MonoBehaviour
{
    [Tooltip("Canvas 단위 UI Screen 이름 (CSV의 type 컬럼과 일치)")]
    public string screen;
}
