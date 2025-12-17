using UnityEngine.Playables;

public class PlayerStateMachine
{
    public Player Player { get; }

    public PlayerLocomotionState Locomotion { get; }
    public PlayerJumpState Jump { get; }
    public PlayerFallState Fall { get; }
    public PlayerAttackState Attack { get; }
    public PlayerDeadState Dead { get; }

    private PlayerState _current;

    public PlayerStateMachine(Player player)
    {
        Player = player;

        Locomotion = new PlayerLocomotionState(this);
        Jump = new PlayerJumpState(this);
        Fall = new PlayerFallState(this);
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

    public void HandleInput() => _current?.HandleInput();
    public void Tick(float dt) => _current?.Tick(dt);
    public void FixedTick(float fdt) => _current?.FixedTick(fdt);
}