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
    private InspectionManager _inspectionManager;

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

        // InputManager 사용
        if (InputManager.Instance == null)
        {
            Debug.LogError("[Player] InputManager.Instance not found", this);
            enabled = false;
            return;
        }

        _inputs = InputManager.Instance.Inputs;         
        _playerActions = _inputs.Player;

        StateMachine = new PlayerStateMachine(this);

        _inspectionManager = GetComponentInChildren<InspectionManager>();
        if (_inspectionManager != null)
        {
            _inspectionManager.Initialize(_inputs);        // Inputs 주입 유지
        }
        else
        {
            Debug.LogError("[Player] InspectionManager not found", this);
        }


        Sfx = GetComponent<PlayerSfxController>();
    }

    private void OnEnable()
    {
        // 플레이어 존재 알림 (InputManager가 Gameplay enable 판단)
        EventBus.Publish(new PlayerPresenceChangedEvent(true));
    }

    private void OnDisable()
    {
        // 플레이어 비존재 알림
        EventBus.Publish(new PlayerPresenceChangedEvent(false));
    }

    // Player는 Input을 소유하지 않음
    // Dispose 책임은 InputManager에 있음

    private void Start()
    {
       if (weaponHandler != null)
            weaponHandler.EquipOnStart();

        StateMachine.ChangeState(StateMachine.Locomotion);
    }

    public PlayerWeaponHandler WeaponHandler => weaponHandler;

    private void Update()
    {
        // Inspection 중 Player FSM 차단
        if (_inspectionManager != null && _inspectionManager.IsInspecting)
            return;

        // ActionMap enable 여부 확인
        if (!_inputs.Player.enabled)
            return;

        ReadInputs();

        StateMachine.HandleInput();
        StateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_inspectionManager != null && _inspectionManager.IsInspecting)
            return;

        if (!_inputs.Player.enabled)
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

    // =========================
    // Inspection
    // =========================
    public void TryEnterInspection(IInspectable inspectable)
    {
        if (_inspectionManager == null) return;
        _inspectionManager.EnterInspection(inspectable);
    }
}