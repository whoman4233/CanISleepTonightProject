using UnityEngine;

public class Player : MonoBehaviour
{
    [field: SerializeField] public PlayerSO Data { get; private set; }
    [field: Header("Animations")]
    [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

    public Animator Animator { get; private set; }
    public PlayerController Input { get; private set; }
    public CharacterController Controller { get; private set; }
    public ForceReceiver ForceReceiver { get; private set; }

    public PlayerStateMachine stateMachine;
    public Health health { get; private set; }
    private void Awake()
    {
        AnimationData.Initialize();

        Animator = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerController>();
        Controller = GetComponent<CharacterController>();
        ForceReceiver = GetComponent<ForceReceiver>();
        stateMachine = new PlayerStateMachine(this);
        health = GetComponent<Health>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // ★ 모든 컴포넌트의 Awake/OnEnable 이 끝난 뒤에 처음 상태로 진입
        stateMachine.ChangeState(stateMachine.IdleState);
        health.OnDie += OnDie;
    }
    private void Update()
    {
        stateMachine.HandleInput();
        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.PhysicsUpdate();
    }
    void OnDie()
    {
        Animator.SetTrigger("Die");
        enabled = false;
    }
}