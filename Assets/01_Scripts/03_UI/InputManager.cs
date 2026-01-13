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
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPlayerPresence);
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);
        EventBus.Unsubscribe(_onGlobalLockRequested);
        EventBus.Unsubscribe(_onGlobalLockReleased);
        EventBus.Unsubscribe(_onInputHardReset);
        EventBus.Unsubscribe(_onGameContextReady);
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            Inputs?.Dispose();
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
        Debug.Log("[InputManager] GameContextReady → Force Input Reset");

        _inspectionActive = false;
        _uiLockCount = 0;

        ApplyState(force: true);
    }

    private void OnInputHardReset(InputHardResetEvent e)
    {
        Debug.Log("[InputManager] InputHardReset");

        _playerPresent = false;
        _inspectionActive = false;
        _uiLockCount = 0;
        _dialogueActive = false;
        _qteActive = false;

        _currentState = InputState.UIOnly;

        SetMap(Inputs.Player, false);
        SetMap(Inputs.Inspection, false);
        SetMap(Inputs.QTE, false);

        // UI는 항상 살아있게 유지
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

        // QTE 중에는 UI 입력 제한
        SetMap(Inputs.UI, next != InputState.QTE);

        ApplyCursor(next);

        Debug.Log($"[InputManager] State={_currentState} lock={_uiLockCount} dialogue={_dialogueActive}");
    }

    private InputState ResolveState()
    {
        if (!_playerPresent)
            return InputState.UIOnly;

        // =========================
        // Dialogue를 UIOnly lock보다 우선
        // - 팝업/락이 걸린 상태에서도 "대화 진행 상태"를 유지
        // - 실제 입력은 UI맵 기반으로 진행
        // =========================
        if (_dialogueActive)
            return InputState.Dialogue;

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
        if (enable && !map.enabled)
            map.Enable();
        else if (!enable && map.enabled)
            map.Disable();
    }

    private static void ApplyCursor(InputState state)
    {
        bool hideCursor =
            state == InputState.Gameplay ||
            state == InputState.QTE;

        Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !hideCursor;
    }

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
}







