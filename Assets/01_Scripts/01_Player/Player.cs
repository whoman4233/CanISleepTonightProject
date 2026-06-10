using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public PlayerInteractor Interactor { get; private set; }
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

    // 현재 이동 속도 (FSM이 계산한 결과)
    public float CurrentMoveSpeed { get; set; }

    // Crouch 전환 중 목표 속도
    public float TargetMoveSpeed { get; set; }

    // 전환 보간 중 현재 속도
    public float SmoothedMoveSpeed { get; set; }

    private PlayerInputs _inputs;
    private PlayerInputs.PlayerActions _playerActions;
    private InspectionManager _inspectionManager;

    // 입력차단 관련 이벤트 핸들러 (캐시)
    private Action<GlobalInputLockRequestedEvent> _onGlobalInputLock;
    private Action<GlobalInputLockReleasedEvent> _onGlobalInputUnlock;
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;
    private Action<DialogueStartedEvent> _onDialogueStarted;
    private Action<DialogueEndedEvent> _onDialogueEnded;
    private Action<PlayerCinematicLockRequestedEvent> _onCinematicLock;
    private Action<PlayerCinematicLockReleasedEvent> _onCinematicUnlock;

    private bool _cinematicLocked; // 연출용 Lock 상태
    private void Awake()
    {
        Interactor = GetComponent<PlayerInteractor>(); // 캐싱용

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

        _onGlobalInputLock = OnGlobalInputLocked;
        _onGlobalInputUnlock = OnGlobalInputUnlocked;
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
        _onDialogueStarted = OnDialogueStarted;
        _onDialogueEnded = OnDialogueEnded;
        _onCinematicLock = OnCinematicLock;
        _onCinematicUnlock = OnCinematicUnlock;
        Sfx = GetComponent<PlayerSfxController>();
    }

    private void OnEnable()
    {
        // 플레이어 존재 알림 (InputManager가 Gameplay enable 판단)
        EventBus.Publish(new PlayerPresenceChangedEvent(true));
        EventBus.Subscribe(_onGlobalInputLock);
        EventBus.Subscribe(_onGlobalInputUnlock);
        EventBus.Subscribe(_onInspectionStarted);
        EventBus.Subscribe(_onInspectionEnded);
        EventBus.Subscribe(_onQTEStarted);
        EventBus.Subscribe(_onQTEEnded);
        EventBus.Subscribe(_onDialogueStarted);
        EventBus.Subscribe(_onDialogueEnded);
        EventBus.Subscribe(_onCinematicLock);
        EventBus.Subscribe(_onCinematicUnlock);
    }

    private void OnDisable()
    {
        // 플레이어 비존재 알림
        EventBus.Publish(new PlayerPresenceChangedEvent(false));
        EventBus.Unsubscribe(_onGlobalInputLock);
        EventBus.Unsubscribe(_onGlobalInputUnlock);
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
        EventBus.Unsubscribe(_onDialogueStarted);
        EventBus.Unsubscribe(_onDialogueEnded);
        EventBus.Unsubscribe(_onCinematicLock);
        EventBus.Unsubscribe(_onCinematicUnlock);
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
        //FSM Pause 중에는 입력 갱신 막아둠
        if (!StateMachine.IsPaused && _inputs.Player.enabled)
        {
            ReadInputs();
        }

        StateMachine.HandleInput();
        StateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
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
        //if (IsCrouchTransitioning)
        //{
        //    MoveInput = Vector2.zero;
        //    AttackPressedThisFrame = false;
        //    CrouchToggleRequested = false;
        //}
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

    // StandUp 시작 시 호출
    public void ExitCrouchModeEarly()
    {
        IsCrouchMode = false;
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

    // =========================
    // Inputmanager 관련
    // =========================
    private bool IsGameplayActive()
    {
        var im = InputManager.Instance;
        if (im == null)
            return false;

        return im.CurrentState == InputState.Gameplay;
    }
    private void ResetInputCache() //기존 FSM 초기화
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        RunHeld = false;

        JumpPressedThisFrame = false;
        AttackPressedThisFrame = false;
        Interaction = false;
        CrouchToggleRequested = false;
    }

    // =========================
    // FSM Pause / Resume 
    // =========================
    private void OnEnterPause()
    {
        // 1. 이동 입력/캐시 제거
        ResetInputCache();

        // 2. CharacterController 이동 정지
        if (Controller != null)
        {
            Controller.Move(Vector3.zero);
        }

        // 3. Animator 이동 파라미터 강제 0
        if (Animator != null && AnimationData != null)
        {
            // 이동 속도 제거
            Animator.SetFloat(AnimationData.SpeedParameterHash, 0f);

            // Blend Tree 입력 제거
            Animator.SetFloat(AnimationData.MoveXParameterHash, 0f);
            Animator.SetFloat(AnimationData.MoveYParameterHash, 0f);

            // Jump / Fall / Land / Attack 은 건드리지 않는다
            // 공중 상태, 공격 상태 보존 목적
        }
            // 4. SFX 정지 (이미 재생 중인 루프)
            if (Sfx != null)
        {
            Sfx.StopFootstepLoopImmediate();
        }
    }


    private void OnGlobalInputLocked(GlobalInputLockRequestedEvent e)
    {
        OnEnterPause();
        StateMachine.SetPaused(true);
    }

    private void OnGlobalInputUnlocked(GlobalInputLockReleasedEvent e)
    {
        StateMachine.SetPaused(false);
    }

    private void OnInspectionStarted(InspectionStartedEvent e)
    {
        OnEnterPause();
        StateMachine.SetPaused(true);
    }

    private void OnInspectionEnded(InspectionEndedEvent e)
    {
        StateMachine.SetPaused(false);
    }
    private void OnQTEStarted(QTEStartedEvent e)
    {
        OnEnterPause();
        StateMachine.SetPaused(true);
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        StateMachine.SetPaused(false);
    }
    private void OnDialogueStarted(DialogueStartedEvent e)
    {
        OnEnterPause();
        StateMachine.SetPaused(true);
    }

    private void OnDialogueEnded(DialogueEndedEvent e)
    {
        StateMachine.SetPaused(false);
    }

    // 플레이어 연출 이벤트 이동 강제용
    private void OnCinematicLock(PlayerCinematicLockRequestedEvent e)
    {
        _cinematicLocked = true;
        StateMachine.SetPaused(true);

        if (Controller != null)
            Controller.enabled = false;
    }

    private void OnCinematicUnlock(PlayerCinematicLockReleasedEvent e)
    {
        _cinematicLocked = false;

        if (Controller != null)
            Controller.enabled = true;

        StateMachine.SetPaused(false);
    }
    public void OnCrouchToggleStarted()
    {
        // 달리기 / 걷기 루프 즉시 정리
        Sfx?.StopFootstepLoopImmediate();
    }
}