using UnityEngine;

public class InputManager : MonoBehaviour
{
    public PlayerInputs SharedInputs { get; private set; }

    private InputMode currentMode = InputMode.Gameplay;

    private bool HasGameFlow => GameManager.Instance != null;

    private bool isPauseMenuOpen;
    private bool isInspecting;

    private void Awake()
    {
        // 테스트 씬 보호: GameManager 없으면 개입하지 않음
        if (!HasGameFlow)
        {
            Debug.Log("[InputManager] GameManager not found. Policy disabled.");
            return;
        }

        SharedInputs = new PlayerInputs();
        SetInputMode(InputMode.Gameplay, true);
    }

    private void OnEnable()
    {
        if (!HasGameFlow)
            return;

        EventBus.Subscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
        EventBus.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
        EventBus.Subscribe<InspectionEndedEvent>(OnInspectionEnded);
        EventBus.Subscribe<InputModeChangedEvent>(OnInputModeChanged);
        EventBus.Subscribe<PauseMenuOpenedEvent>(OnPauseMenuOpened);
        EventBus.Subscribe<PauseMenuClosedEvent>(OnPauseMenuClosed);
    }

    private void OnDisable()
    {
        if (!HasGameFlow)
            return;

        EventBus.Unsubscribe<GamePhaseChangedEvent>(OnGamePhaseChanged);
        EventBus.Unsubscribe<InspectionStartedEvent>(OnInspectionStarted);
        EventBus.Unsubscribe<InspectionEndedEvent>(OnInspectionEnded);
        EventBus.Unsubscribe<InputModeChangedEvent>(OnInputModeChanged);
    }

    private void OnGamePhaseChanged(GamePhaseChangedEvent e)
    {
        if (isPauseMenuOpen)
            return;

        if (e.Phase == GamePhase.NotStarted)
            SetInputMode(InputMode.UIOnly);
        else
            SetInputMode(InputMode.Gameplay);
    }


    private void OnInspectionStarted(InspectionStartedEvent e)
    {
        isInspecting = true;

        if (!isPauseMenuOpen)
            SetInputMode(InputMode.Inspection);
    }

    private void OnInspectionEnded(InspectionEndedEvent e)
    {
        isInspecting = false;

        if (!isPauseMenuOpen)
            SetInputMode(InputMode.Gameplay);
    }


    private void OnInputModeChanged(InputModeChangedEvent e)
    {
        SetInputMode(e.Mode);
    }

    private void SetInputMode(InputMode mode, bool force = false)
    {
        if (!HasGameFlow)
            return;

        if (!force && currentMode == mode)
            return;

        currentMode = mode;

        SharedInputs.Player.Disable();
        SharedInputs.UI.Disable();
        SharedInputs.Inspection.Disable();

        switch (mode)
        {
            case InputMode.Gameplay:
                SharedInputs.Player.Enable();
                ApplyCursor(true);
                break;

            case InputMode.UIOnly:
                SharedInputs.UI.Enable();
                ApplyCursor(false);
                break;

            case InputMode.Inspection:
                SharedInputs.Inspection.Enable();
                ApplyCursor(false);
                break;
        }

        Debug.Log($"[InputManager] InputMode = {mode}");
    }

    private void ApplyCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnPauseMenuOpened(PauseMenuOpenedEvent e)
    {
        isPauseMenuOpen = true;
        SetInputMode(InputMode.UIOnly);
    }

    private void OnPauseMenuClosed(PauseMenuClosedEvent e)
    {
        isPauseMenuOpen = false;

        if (isInspecting)
            SetInputMode(InputMode.Inspection);
        else
            SetInputMode(InputMode.Gameplay);
    }

}



