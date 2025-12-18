using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [field: SerializeField] public PlayerSO Data { get; private set; }
    [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

    public Animator Animator { get; private set; }
    public CharacterController Controller { get; private set; }
    public ForceReceiver ForceReceiver { get; private set; }

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

    private PlayerInputs _inputs;
    private PlayerInputs.PlayerActions _playerActions;
    public PlayerInputs Inputs => _inputs;

    private bool _isInspectionLocked; // Inspection(상세보기) 중 플레이어 입력 차단 플래그

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

        _inputs = new PlayerInputs();
        _playerActions = _inputs.Player;

        StateMachine = new PlayerStateMachine(this);
    }
    private void OnEnable()
    {
        _inputs.Enable();
        _inputs.Player.Enable();
        _inputs.Inspection.Disable();

        //상세보기 진입 구독
        EventBus.Subscribe<InspectionStartedEvent>(OnInspectionStarted); 
        EventBus.Subscribe<InspectionEndedEvent>(OnInspectionEnded);
    }
    private void OnDisable()
    {
        _inputs.Disable();

        //상세보기 진입 구독해지
        EventBus.Unsubscribe<InspectionStartedEvent>(OnInspectionStarted);
        EventBus.Unsubscribe<InspectionEndedEvent>(OnInspectionEnded);
    }

    private void OnDestroy()
    {
        _inputs?.Dispose();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        StateMachine.ChangeState(StateMachine.Locomotion);
    }

    private void Update()
    {
        if (_isInspectionLocked) // 상세보기 진입시 플레이어 입력 잠그기
            return;

        ReadInputs();

        StateMachine.HandleInput();
        StateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_isInspectionLocked) // 상세보기 진입시 플레이어 입력 잠그기
            return;

        StateMachine.FixedTick(Time.fixedDeltaTime);
    }

    private void ReadInputs()
    {
        // Walk: Vector2, Run: Button, Look: Delta, Jump/Attack: Button
        MoveInput = _playerActions.Walk.ReadValue<Vector2>();
        LookInput = _playerActions.Look.ReadValue<Vector2>();
        RunHeld = _playerActions.Run.IsPressed();

        JumpPressedThisFrame = _playerActions.Jump.WasPressedThisFrame();
        AttackPressedThisFrame = _playerActions.Attack.WasPressedThisFrame();
    }

    private void OnInspectionStarted(InspectionStartedEvent evt) // 상세보기 이벤트 진입 핸들러
    {
        _isInspectionLocked = true;

        _inputs.Player.Disable();

        //이미 입력된 잔상 제거
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        RunHeld = false;
        JumpPressedThisFrame = false;
        AttackPressedThisFrame = false;

        StateMachine.ChangeState(StateMachine.Locomotion);
    }

    private void OnInspectionEnded(InspectionEndedEvent evt) // 상세보기 이벤트 종료 핸들러
    {
        _isInspectionLocked = false;
        _inputs.Player.Enable();
    }
}