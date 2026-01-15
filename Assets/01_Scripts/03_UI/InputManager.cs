using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerInputs Inputs { get; private set; }

    private bool _playerPresent;
    private bool _inspectionActive;
    private int _uiLockCount;
    private bool _dialogueActive;
    private bool _qteActive;

    private bool _cursorOverridden;
    private bool _overrideHideCursor;
    private CursorLockMode _overrideLockMode;
    private InputState _currentState;
    public InputState CurrentState => _currentState;

    private Action<PlayerPresenceChangedEvent> _onPlayerPresence;
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;
    private Action<GlobalInputLockRequestedEvent> _onGlobalLockRequested;
    private Action<GlobalInputLockReleasedEvent> _onGlobalLockReleased;
    private Action<InputHardResetEvent> _onInputHardReset;
    private Action<GameContextReadyEvent> _onGameContextReady;
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;
    private Action<CursorOverrideRequestedEvent> _onCursorOverride;
    private Action<CursorOverrideReleasedEvent> _onCursorOverrideReleased;
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

        // UI 입력은 항상 Enable
        Inputs.UI.Enable();

        _onPlayerPresence = OnPlayerPresence;
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
        _onGlobalLockRequested = OnGlobalLockRequested;
        _onGlobalLockReleased = OnGlobalLockReleased;
        _onInputHardReset = OnInputHardReset;
        _onGameContextReady = OnGameContextReady;
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
        _onCursorOverride = OnCursorOverrideRequested;
        _onCursorOverrideReleased = OnCursorOverrideReleased;
        ApplyState(force: true);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPlayerPresence);
        EventBus.Subscribe(_onInspectionStarted);
        EventBus.Subscribe(_onInspectionEnded);
        EventBus.Subscribe(_onGlobalLockRequested);
        EventBus.Subscribe(_onGlobalLockReleased);
        EventBus.Subscribe(_onInputHardReset);
        EventBus.Subscribe(_onGameContextReady);
        EventBus.Subscribe(_onQTEStarted);
        EventBus.Subscribe(_onQTEEnded);
        EventBus.Subscribe(_onCursorOverride);
        EventBus.Subscribe(_onCursorOverrideReleased);
        Inputs.UI.Click.performed += OnUIClick;
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        if (Inputs != null)
        {
            Inputs.UI.Click.performed -= OnUIClick;
        }
        EventBus.Unsubscribe(_onPlayerPresence);
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);
        EventBus.Unsubscribe(_onGlobalLockRequested);
        EventBus.Unsubscribe(_onGlobalLockReleased);
        EventBus.Unsubscribe(_onInputHardReset);
        EventBus.Unsubscribe(_onGameContextReady);
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
        EventBus.Unsubscribe(_onCursorOverride);
        EventBus.Unsubscribe(_onCursorOverrideReleased);
        Inputs.UI.Click.performed -= OnUIClick;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            Inputs?.Dispose();
    }
    private void OnCursorOverrideRequested(CursorOverrideRequestedEvent e)
    {
        _cursorOverridden = true;
        _overrideHideCursor = e.HideCursor;
        _overrideLockMode = e.LockMode;

        ApplyCursor(_currentState);
    }

    private void OnCursorOverrideReleased(CursorOverrideReleasedEvent e)
    {
        _cursorOverridden = false;
        ApplyCursor(_currentState);
    }

    private void OnPlayerPresence(PlayerPresenceChangedEvent e)
    {
        _playerPresent = e.IsPresent;
        ApplyState();
    }

    private void OnInspectionStarted(InspectionStartedEvent e)
    {
        _inspectionActive = true;
        ApplyState();
    }

    private void OnInspectionEnded(InspectionEndedEvent e)
    {
        _inspectionActive = false;
        ApplyState();
    }

    private void OnGlobalLockRequested(GlobalInputLockRequestedEvent e)
    {
        _uiLockCount++;
        ApplyState();
    }

    private void OnGlobalLockReleased(GlobalInputLockReleasedEvent e)
    {
        _uiLockCount = Mathf.Max(0, _uiLockCount - 1);
        ApplyState();
    }

    private void OnGameContextReady(GameContextReadyEvent e)
    {
        _inspectionActive = false;
        _uiLockCount = 0;
        ApplyState(force: true);
    }

    private void OnInputHardReset(InputHardResetEvent e)
    {
        _playerPresent = false;
        _inspectionActive = false;
        _uiLockCount = 0;
        _dialogueActive = false;
        _qteActive = false;

        _currentState = InputState.UIOnly;

        SetMap(Inputs.Player, false);
        SetMap(Inputs.Inspection, false);
        SetMap(Inputs.QTE, false);

        // =========================
        // [중요] Dialogue ActionMap은 InputManager에서 제어하지 않는다
        // =========================
        // SetMap(Inputs.Dialogue, false);  // [제거하지 않고 주석만 설명]

        // UI는 기본 Enable
        SetMap(Inputs.UI, true);

        ApplyCursor(InputState.UIOnly);
    }

    private void OnQTEStarted(QTEStartedEvent e)
    {
        _qteActive = true;
        ApplyState(force: true);
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        _qteActive = false;
        ApplyState();
    }

    private void ApplyState(bool force = false)
    {
        InputState next = ResolveState();

        if (!force && next == _currentState)
            return;

        _currentState = next;

        SetMap(Inputs.Player, next == InputState.Gameplay);
        SetMap(Inputs.Inspection, next == InputState.Inspection);
        SetMap(Inputs.QTE, next == InputState.QTE);

        // =========================
        // [중요] Dialogue ActionMap 제거
        // - Dialogue 입력은 DialogueManager 전담
        // =========================
        // SetMap(Inputs.Dialogue, next == InputState.Dialogue); // [수정]

        // QTE 중 UI 제한
        SetMap(Inputs.UI, next != InputState.QTE);

        ApplyCursor(next);
    }

    private InputState ResolveState()
    {
        // =========================
        // Dialogue는 "플레이어 입력 차단" 용도로만 사용
        // =========================
        if (_dialogueActive)
            return InputState.UIOnly; // [수정]

        if (!_playerPresent)
            return InputState.UIOnly;

        if (_uiLockCount > 0)
            return InputState.UIOnly;

        if (_inspectionActive)
            return InputState.Inspection;

        if (_qteActive)
            return InputState.QTE;

        return InputState.Gameplay;
    }

    private static void SetMap(InputActionMap map, bool enable)
    {
        if (enable && !map.enabled) map.Enable();
        else if (!enable && map.enabled) map.Disable();
    }

    private void ApplyCursor(InputState state)
    {
        if (_cursorOverridden)
        {
            Cursor.lockState = _overrideLockMode;
            Cursor.visible = !_overrideHideCursor;
            return;
        }

        bool hideCursor =
            state == InputState.Gameplay ||
            state == InputState.QTE;

        Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !hideCursor;
    }


    // =========================
    // [중요] Dialogue 상태는 Player 차단용 플래그로만 사용
    // =========================
    public void SetDialogueActive(bool isActive)
    {
        _dialogueActive = isActive;
        ApplyState();
    }

    public void ResetPlayerInputs()
    {
        Inputs.Player.Disable();
        Inputs.Player.Enable();
    }

    // =========================
    // UI용 클릭 이벤트
    // =========================
    private void OnUIClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        // UIOnly 상태에서만 의미 있는 이벤트
        if (_currentState != InputState.UIOnly)
            return;

        EventBus.Publish(new UIProceedRequestedEvent());
    }
}










