public enum GlobalInputLockReason
{
    PauseMenu,
    ModalPopup,
    ResultScreen,
    Other
}
public enum InputState
{
    Gameplay,      // 플레이어 조작
    Inspection,    // 상세보기
    UIOnly         // 메뉴 / 결과 / 팝업
}