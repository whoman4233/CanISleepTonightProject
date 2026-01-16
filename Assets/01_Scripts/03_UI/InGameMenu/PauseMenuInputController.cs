using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuInputController : MonoBehaviour
{
    private PlayerInputs _inputs;

    private bool _menuOpen;
    private bool _playerPresent;
    private bool _escSubscribed;

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
        EventBus.Subscribe(_onMenuOpened);
        EventBus.Subscribe(_onMenuClosed);
        EventBus.Subscribe(_onPlayerPresence);
    }

    private void OnDisable()
    {
        UnsubscribeEsc();

        EventBus.Unsubscribe(_onMenuOpened);
        EventBus.Unsubscribe(_onMenuClosed);
        EventBus.Unsubscribe(_onPlayerPresence);
    }

    private void OnPauseMenuOpened(PauseMenuOpenedEvent e) => _menuOpen = true;
    private void OnPauseMenuClosed(PauseMenuClosedEvent e) => _menuOpen = false;

    private void OnPlayerPresenceChanged(PlayerPresenceChangedEvent e)
    {
        _playerPresent = e.IsPresent;

        // =========================
        // 플레이어 상태에 따라 ESC 입력 활성/비활성
        // =========================
        if (_playerPresent)
        {
            SubscribeEsc();
        }
        else
        {
            UnsubscribeEsc();

            // Intro로 돌아갔는데 메뉴가 열린 상태면 닫기 요청
            if (_menuOpen)
            {
                EventBus.Publish(new PauseMenuCloseRequestedEvent());
            }
        }
    }

    // =========================
    // [ADDED] ESC 구독 관리
    // =========================
    private void SubscribeEsc()
    {
        if (_escSubscribed)
            return;

        _inputs.UI.Setting.performed += OnEsc;
        _escSubscribed = true;
    }

    private void UnsubscribeEsc()
    {
        if (!_escSubscribed)
            return;

        _inputs.UI.Setting.performed -= OnEsc;
        _escSubscribed = false;
    }

    private void OnEsc(InputAction.CallbackContext ctx)
    {
        // 1) Popup이 열려 있으면 Popup부터 닫기
        if (popupRoot != null && popupRoot.HasAnyPopupOpen)
        {
            EventBus.Publish(new PopupCloseRequestedEvent());
            return;
        }

        // 2) InGameMenu 토글 (이 시점엔 플레이어 있음이 보장됨)
        if (!_menuOpen)
            EventBus.Publish(new PauseMenuOpenRequestedEvent());
        else
            EventBus.Publish(new PauseMenuCloseRequestedEvent());
    }
}





