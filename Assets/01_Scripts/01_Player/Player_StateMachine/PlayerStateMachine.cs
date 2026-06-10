using UnityEngine.Playables;

public class PlayerStateMachine
{
    public Player Player { get; }

    public PlayerLocomotionState Locomotion { get; }
    public PlayerJumpState Jump { get; }
    public PlayerFallState Fall { get; }
    public PlayerLandState Land { get; }
    public PlayerAttackState Attack { get; }
    public PlayerDeadState Dead { get; }

    private PlayerState _current;

    public float CurrentSpeed { get; private set; }

    private bool _isPaused; //FSM Pause 플래그
    public bool IsPaused => _isPaused;
    public PlayerStateMachine(Player player)
    {
        Player = player;

        Locomotion = new PlayerLocomotionState(this);
        Jump = new PlayerJumpState(this);
        Fall = new PlayerFallState(this);
        Land = new PlayerLandState(this);
        Attack = new PlayerAttackState(this);
        Dead = new PlayerDeadState(this);
    }

    public void ChangeState(PlayerState next)
    {
        if (_current == next) return;

        _current?.Exit();
        _current = next;
        _current.Enter();
    }

    // =========================
    // Pause 제어 API
    // =========================
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    public void HandleInput()
    {
        if (_isPaused) return;
        _current?.HandleInput();
    }

    public void Tick(float dt)
    {
        if (_isPaused) return;
        _current?.Tick(dt);
    }

    public void FixedTick(float fdt) // 정지 해제 후 움직임보간
    {
        if (_isPaused) return;
        _current?.FixedTick(fdt);
    }
    public void SetCurrentSpeed(float speed)
    {
        CurrentSpeed = speed;
    }
}