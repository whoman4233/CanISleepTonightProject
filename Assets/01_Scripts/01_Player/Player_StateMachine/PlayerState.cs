using UnityEngine;

public abstract class PlayerState
{
    protected readonly PlayerStateMachine SM;
    protected Player P => SM.Player;

    protected PlayerState(PlayerStateMachine sm) => SM = sm;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void HandleInput() { }
    public virtual void Tick(float dt) { }
    public virtual void FixedTick(float fdt) { }

    protected bool IsGrounded => P.Controller != null && P.Controller.isGrounded;

}