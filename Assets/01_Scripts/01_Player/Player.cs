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

    public PlayerStateMachine StateMachine { get; private set; }
    public Health health { get; private set; }
    private void Awake()
    {
        AnimationData.Initialize();

        Animator = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerController>();
        Controller = GetComponent<CharacterController>();
        ForceReceiver = GetComponent<ForceReceiver>();
        StateMachine = new PlayerStateMachine(this);
        health = GetComponent<Health>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        health.OnDie += OnDie;
        StartStateMachine();
    }
    private void Update()
    {
        StateMachine.HandleInput();
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.PhysicsUpdate();
    }
    void OnDie()
    {
        Animator.SetTrigger("Die");
        enabled = false;
    }
    public void StartStateMachine()
    {
        StateMachine.ChangeState(StateMachine.IdleState);
    }
}