using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuInputController : MonoBehaviour
{
    private PlayerInputs _inputs;
    private bool _menuOpen;

    // WeakReference EventBus 대응: 구독 콜백을 필드로 강참조 유지
    private Action<PauseMenuOpenedEvent> _onMenuOpened;
    private Action<PauseMenuClosedEvent> _onMenuClosed;

    private void Awake()
    {
        // InputManager.Instance가 Awake 시점에 없을 수 있으므로 OnEnable에서도 방어 처리
        if (InputManager.Instance != null)
            _inputs = InputManager.Instance.Inputs;

        _onMenuOpened = _ => _menuOpen = true;
        _onMenuClosed = _ => _menuOpen = false;
    }

    private void OnEnable()
    {
        if (_inputs == null && InputManager.Instance != null)
            _inputs = InputManager.Instance.Inputs;

        if (_inputs != null)
            _inputs.UI.Setting.performed += OnEsc;

        EventBus.Subscribe(_onMenuOpened);
        EventBus.Subscribe(_onMenuClosed);
    }

    private void OnDisable()
    {
        if (_inputs != null)
            _inputs.UI.Setting.performed -= OnEsc;

        EventBus.Unsubscribe(_onMenuOpened);
        EventBus.Unsubscribe(_onMenuClosed);
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        
        if (!_menuOpen)
            EventBus.Publish(new PauseMenuOpenRequestedEvent());
        else
            EventBus.Publish(new PauseMenuCloseRequestedEvent());
    }
}


