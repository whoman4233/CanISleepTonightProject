using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [field: SerializeField] public PlayerSO Data { get; private set; }
    [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

    [Header("Refs (Cache)")]
    [SerializeField] private PlayerWeaponHandler weaponHandler;
    public PlayerSfxController Sfx { get; private set; }
    public Animator Animator { get; private set; }
    public CharacterController Controller { get; private set; }
    public ForceReceiver ForceReceiver { get; private set; }
    public bool Interaction { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }

    // PlayerInputs 기반 입력 캐시 (FSM이 읽어감)
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool RunHeld { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public float AirStartY { get; set; }
    public float AirApexY { get; set; }
    public bool AirFromJump { get; set; }
    public bool JumpLocked { get; set; }
    public bool AttackPressedThisFrame { get; private set; }

    // ---- Crouch ----
    public bool IsCrouching { get; set; }                  // Animator Bool과 동기화 용도
    public bool CrouchToggleRequested { get; set; }        // Ctrl 토글 요청

    // CrouchDown / StandUp 재생 중(=전환 애니메이션 중)
    public bool IsCrouchTransitioning { get; private set; }

    // 앉은 자세 유지 중(=CrouchLocomotion 상태 유지 의미)
    public bool IsCrouchMode { get; private set; }

    // 점프는 "전환 중" + "앉은 자세 유지" 둘 다 막아야 함
    public bool IsJumpBlockedByCrouch => IsCrouchTransitioning || IsCrouchMode;

    private PlayerInputs _inputs;
    private PlayerInputs.PlayerActions _playerActions;
    public PlayerInputs Inputs => _inputs;
    private InputMode _currentInputMode = InputMode.Gameplay;
    private InspectionManager _inspectionManager;

    //테스트 보호용 bool
    private bool HasInputPolicy => FindObjectOfType<InputManager>() != null;
    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        Controller = GetComponent<CharacterController>();
        ForceReceiver = GetComponent<ForceReceiver>();

        if (AnimationData == null)
        {
            Debug.LogError("[Player] AnimationData가 비어있습니다. Inspector에서 할당하세요.", this);
            enabled = false;
            return;
        }

        AnimationData.Initialize();

        var inputManager = FindObjectOfType<InputManager>();

        if (inputManager != null && inputManager.SharedInputs != null)
        {
            _inputs = inputManager.SharedInputs;
        }
        else
        {
            // 테스트/단독 실행용 Fallback
            _inputs = new PlayerInputs();
            _inputs.Player.Enable();

            Debug.LogWarning("[Player] InputManager 찾을 수 없음. Fallback input enabled.");
        }

        _playerActions = _inputs.Player;

        _playerActions.Setting.performed += OnSettingsPressed;

        StateMachine = new PlayerStateMachine(this);

        _inspectionManager = GetComponentInChildren<InspectionManager>();
        if (_inspectionManager == null)
        {
            Debug.LogError("[Player] InspectionManager not found", this);
        }
        else
        {
            _inspectionManager.Initialize(_inputs);
        }

        Sfx = GetComponent<PlayerSfxController>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InputModeChangedEvent>(OnInputModeChanged);
    }

    private void OnDisable()
    {
        _playerActions.Setting.performed -= OnSettingsPressed;
        EventBus.Unsubscribe<InputModeChangedEvent>(OnInputModeChanged);
    }

    private void OnDestroy()
    {
        _inputs?.Dispose();
    }

    private void Start()
    {
       if (weaponHandler != null)
            weaponHandler.EquipOnStart();

        StateMachine.ChangeState(StateMachine.Locomotion);
    }

    public PlayerWeaponHandler WeaponHandler => weaponHandler;

    private void Update()
    {
        // Inspection 중이면 무조건 차단
        if (_inspectionManager != null && _inspectionManager.IsInspecting)
            return;
        // 정책이 있을 때만 모드 제한
        if (HasInputPolicy && _currentInputMode != InputMode.Gameplay)
            return;
        ReadInputs();

        StateMachine.HandleInput();
        StateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_inspectionManager != null && _inspectionManager.IsInspecting)
            return;

        if (HasInputPolicy && _currentInputMode != InputMode.Gameplay)
            return;

        StateMachine.FixedTick(Time.fixedDeltaTime);
    }

    private void ReadInputs()
    {
        // 기본 입력 읽기
        MoveInput = _playerActions.Walk.ReadValue<Vector2>();
        LookInput = _playerActions.Look.ReadValue<Vector2>();
        RunHeld = _playerActions.Run.IsPressed();

        JumpPressedThisFrame = _playerActions.Jump.WasPressedThisFrame();
        AttackPressedThisFrame = _playerActions.Attack.WasPressedThisFrame();
        Interaction = _playerActions.Interaction.WasPressedThisFrame();
        CrouchToggleRequested = _playerActions.Crouch.WasPressedThisFrame();

        // 앉기 시작~서기 끝까지 점프 차단
        if (IsJumpBlockedByCrouch)
            JumpPressedThisFrame = false;

        // 전환 애니메이션 중 이동/공격/점프 잠금
        if (IsCrouchTransitioning)
        {
            MoveInput = Vector2.zero;
            AttackPressedThisFrame = false;
            CrouchToggleRequested = false;
        }
    }

    private void ResetFrameInputs()
    {
        JumpPressedThisFrame = false;
        AttackPressedThisFrame = false;
        Interaction = false;
        CrouchToggleRequested = false;
    }

    // Locomotion에서 "트리거 쏘는 순간" 전환 잠금을 즉시 시작하기 위한 함수
    public void BeginCrouchTransitionLock()
    {
        IsCrouchTransitioning = true;
        ResetFrameInputs();
    }

    // ---- Animation Events ----
    public void AE_BeginCrouchTransition()
    {
        IsCrouchTransitioning = true;
    }

    public void AE_EndCrouchTransition()
    {
        IsCrouchTransitioning = false;
    }

    public void AE_EndCrouchDown()
    {
        IsCrouchMode = true;
    }

    public void AE_EndStandUp()
    {
        IsCrouchMode = false;
    }
    public void ForceClearCrouchTransitionLock()
    {
        // 전환 애니가 공중 전환으로 끊겼을 때를 대비한 안전장치
        IsCrouchTransitioning = false;
        CrouchToggleRequested = false;
    }
    public void ForceResetCrouchToStanding()
    {
        // 앉기 관련 상태를 "서있는 기본 상태"로 강제 동기화
        IsCrouchTransitioning = false;
        IsCrouchMode = false;
        CrouchToggleRequested = false;

        IsCrouching = false;

        if (Animator != null)
        {
            Animator.SetBool(AnimationData.IsCrouchingParameterHash, false);
        }
    }

    public void TryEnterInspection(IInspectable inspectable)
    {
        if (_inspectionManager == null)
            return;

        _inspectionManager.EnterInspection(inspectable);
    }

    private void OnSettingsPressed(InputAction.CallbackContext context)
    {
        //  Inspection 중에는 무조건 무시
        if (_inspectionManager != null && _inspectionManager.IsInspecting)
            return;
        if (HasInputPolicy && _currentInputMode == InputMode.Inspection)
            return;

        EventBus.Publish(new PauseMenuToggleRequestedEvent());
    }

    private void OnInputModeChanged(InputModeChangedEvent e)
    {
        _currentInputMode = e.Mode;
    }
}