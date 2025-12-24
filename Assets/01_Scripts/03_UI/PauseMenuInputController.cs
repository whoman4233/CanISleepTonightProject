using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuInputController : MonoBehaviour
{
    private PlayerInputs _inputs;
    private bool _menuOpen;

    // [추가] PopupRoot 참조
    private PopupCanvasRootController popupRoot;

    private void Awake()
    {
        _inputs = InputManager.Instance.Inputs;

        // [추가] PopupRoot 캐싱
        popupRoot = FindObjectOfType<PopupCanvasRootController>();
    }

    private void OnEnable()
    {
        _inputs.UI.Setting.performed += OnEsc;
        EventBus.Subscribe<PauseMenuOpenedEvent>(_ => _menuOpen = true);
        EventBus.Subscribe<PauseMenuClosedEvent>(_ => _menuOpen = false);
    }

    private void OnDisable()
    {
        _inputs.UI.Setting.performed -= OnEsc;
        EventBus.Unsubscribe<PauseMenuOpenedEvent>(_ => { });
        EventBus.Unsubscribe<PauseMenuClosedEvent>(_ => { });
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        // =========================
        // [핵심 추가] Popup 우선 처리
        // =========================
        if (popupRoot != null && popupRoot.HasAnyPopupOpen)
        {
            EventBus.Publish(new PopupCloseRequestedEvent());
            return; // ESC 소비
        }

        // =========================
        // [기존 로직] InGameMenu 처리
        // =========================
        if (!_menuOpen)
            EventBus.Publish(new PauseMenuOpenRequestedEvent());
        else
            EventBus.Publish(new PauseMenuCloseRequestedEvent());
    }
}



