using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuInputController : MonoBehaviour
{
    private PlayerInputs _inputs;

    private bool _menuOpen;
    private bool _playerPresent;

    private PopupCanvasRootController popupRoot;

    private Action<PauseMenuOpenedEvent> _onMenuOpened;
    private Action<PauseMenuClosedEvent> _onMenuClosed;
    private Action<PlayerPresenceChangedEvent> _onPlayerPresence;

    private void Awake()
    {
        _inputs = InputManager.Instance.Inputs;

        popupRoot = FindObjectOfType<PopupCanvasRootController>();

        _onMenuOpened = OnPauseMenuOpened;
        _onMenuClosed = OnPauseMenuClosed;
        _onPlayerPresence = OnPlayerPresenceChanged;
    }

    private void OnEnable()
    {
        _inputs.UI.Setting.performed += OnEsc;

        EventBus.Subscribe(_onMenuOpened);
        EventBus.Subscribe(_onMenuClosed);
        EventBus.Subscribe(_onPlayerPresence);
    }

    private void OnDisable()
    {
        _inputs.UI.Setting.performed -= OnEsc;

        EventBus.Unsubscribe(_onMenuOpened);
        EventBus.Unsubscribe(_onMenuClosed);
        EventBus.Unsubscribe(_onPlayerPresence);
    }

    private void OnPauseMenuOpened(PauseMenuOpenedEvent e) => _menuOpen = true;
    private void OnPauseMenuClosed(PauseMenuClosedEvent e) => _menuOpen = false;

    private void OnPlayerPresenceChanged(PlayerPresenceChangedEvent e)
    {
        _playerPresent = e.IsPresent;

        // Intro(플레이어 없음)로 돌아갔는데 메뉴가 열린 상태로 남아있으면 닫기 요청
        if (!_playerPresent && _menuOpen)
        {
            EventBus.Publish(new PauseMenuCloseRequestedEvent());
        }
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        // 1) Popup이 열려 있으면 Popup부터 닫기
        if (popupRoot != null && popupRoot.HasAnyPopupOpen)
        {
            EventBus.Publish(new PopupCloseRequestedEvent());
            return;
        }

        // 2) 플레이어가 없으면(=Intro/MainMenu 컨텍스트) ESC로 인게임메뉴를 절대 열지 않는다
        if (!_playerPresent)
            return;

        // 3) InGameMenu 토글
        if (!_menuOpen)
            EventBus.Publish(new PauseMenuOpenRequestedEvent());
        else
            EventBus.Publish(new PauseMenuCloseRequestedEvent());
    }
}





