using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerInputs Inputs { get; private set; }

    private bool _playerPresent;
    private bool _inspectionActive;
    private int _uiLockCount; // Pause / Popup / Result 등

    private InputState _currentState;
    public InputState CurrentState => _currentState;
    // =============================
    // EventBus handlers (캐시)
    // =============================
    private Action<PlayerPresenceChangedEvent> _onPlayerPresence;
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;
    private Action<GlobalInputLockRequestedEvent> _onGlobalLockRequested;
    private Action<GlobalInputLockReleasedEvent> _onGlobalLockReleased;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Inputs = new PlayerInputs();

        // UI 입력은 항상 켜둔다 (핵심 정책)
        Inputs.UI.Enable();

        // =============================
        // Event handler 캐싱 (중요)
        // =============================
        _onPlayerPresence = OnPlayerPresence;

        _onInspectionStarted = _ =>
        {
            _inspectionActive = true;
            ApplyState();
        };

        _onInspectionEnded = _ =>
        {
            _inspectionActive = false;
            ApplyState();
        };

        _onGlobalLockRequested = _ =>
        {
            _uiLockCount++;
            ApplyState();
        };

        _onGlobalLockReleased = _ =>
        {
            _uiLockCount = Mathf.Max(0, _uiLockCount - 1);
            ApplyState();
        };

        ApplyState(force: true);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPlayerPresence);
        EventBus.Subscribe(_onInspectionStarted);
        EventBus.Subscribe(_onInspectionEnded);
        EventBus.Subscribe(_onGlobalLockRequested);
        EventBus.Subscribe(_onGlobalLockReleased);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPlayerPresence);
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);
        EventBus.Unsubscribe(_onGlobalLockRequested);
        EventBus.Unsubscribe(_onGlobalLockReleased);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            Inputs?.Dispose();
        }
    }

    // =============================
    // Event handlers
    // =============================
    private void OnPlayerPresence(PlayerPresenceChangedEvent e)
    {
        _playerPresent = e.IsPresent;
        ApplyState();
    }

    // =============================
    // Core: 상태 계산
    // =============================
    private void ApplyState(bool force = false)
    {
        InputState next = ResolveState();

        if (!force && next == _currentState)
            return;

        _currentState = next;

        // Gameplay / Inspection만 Enable/Disable
        SetMap(Inputs.Player, next == InputState.Gameplay);
        SetMap(Inputs.Inspection, next == InputState.Inspection);

        ApplyCursor(next);

        Debug.Log($"[InputManager] State={_currentState} UIAlwaysOn lock={_uiLockCount}");
    }

    private InputState ResolveState()
    {
        if (!_playerPresent)
            return InputState.UIOnly;

        if (_uiLockCount > 0)
            return InputState.UIOnly;

        if (_inspectionActive)
            return InputState.Inspection;

        return InputState.Gameplay;
    }

    private static void SetMap(InputActionMap map, bool enable)
    {
        if (enable && !map.enabled) map.Enable();
        if (!enable && map.enabled) map.Disable();
    }

    private static void ApplyCursor(InputState state)
    {
        bool gameplay = state == InputState.Gameplay;
        Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplay;
    }
}





