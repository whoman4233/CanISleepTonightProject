using UnityEngine;

public class PrisonerActionIdleState : BasePrisonerState
{
    private PrisonerAIType _currentType;
    private float _noiseTimer = 0f;

    public PrisonerActionIdleState(PrisonerFSM fsm) : base(fsm) { }

    public void SetActionType(PrisonerAIType aiType)
    {
        _currentType = aiType;
    }

    public override void Enter()
    {
        base.Enter();
        // ★ 복귀 로직 삭제됨 (ReturnState가 처리함)
        // 즉시 행동 시작
        StartActionBehavior();
    }

    public override void Update()
    {
        // ★ 이동 관련 로직 삭제됨

        switch (_currentType)
        {
            case PrisonerAIType.Singing:
            case PrisonerAIType.Screaming:
                _noiseTimer += Time.deltaTime;
                if (_noiseTimer > 3.0f) { _noiseTimer = 0f; }
                break;

            case PrisonerAIType.Ambusher:
                if (player != null && Vector3.Distance(Controller.transform.position, player.position) < 3.5f)
                {
                    Debug.Log($"[Ambush] {Controller.Data.ID} 기습 시작!");
                    PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
                    fsm.ChangeState(fsm.CombatState);
                }
                break;
        }
    }

    private void StartActionBehavior()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
        Controller.StartActionBehavior(_currentType);
    }

    public override void Exit()
    {
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // ... (기존 피격 로직 유지) ...
        if (_currentType == PrisonerAIType.Bad || _currentType == PrisonerAIType.Ambusher) // 예시
            fsm.ChangeState(fsm.CombatState);
        else
            fsm.ChangeState(fsm.CowerState);
    }
}