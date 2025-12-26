using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuInputController : MonoBehaviour
{
    private PlayerInputs _inputs;
    private bool _menuOpen;

    private Action<PauseMenuOpenedEvent> _onOpened;
    private Action<PauseMenuClosedEvent> _onClosed;

    
    private PopupCanvasRootController popupRoot;

    private void Awake()
    {
        _inputs = InputManager.Instance.Inputs;

        
        popupRoot = FindObjectOfType<PopupCanvasRootController>();
        _onOpened = _ => _menuOpen = true;
        _onClosed = _ => _menuOpen = false;
    }

    private void OnEnable()
    {
        _inputs.UI.Setting.performed += OnEsc;
        EventBus.Subscribe(_onOpened);
        EventBus.Subscribe(_onClosed);
    }

    private void OnDisable()
    {
        _inputs.UI.Setting.performed -= OnEsc;
        EventBus.Unsubscribe(_onOpened);
        EventBus.Unsubscribe(_onClosed);
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



