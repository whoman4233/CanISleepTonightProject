using UnityEngine;

public class PrisonerActionIdleState : BasePrisonerState
{
    private PrisonerAIType _currentType;
    private float _noiseTimer = 0f;

    public PrisonerActionIdleState(PrisonerFSM fsm) : base(fsm) { }

    // FSM 초기화 시 어떤 행동을 할지 설정
    public void SetActionType(PrisonerAIType aiType)
    {
        _currentType = aiType;
    }

    public override void Enter()
    {
        base.Enter();

        // 1. 이동 완전 정지 (공통)
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // 2. 컨트롤러에게 행동 개시 명령 (애니메이션, 소리, 프롭)
        // 일반(Good/Bad)인 경우 _currentType이 0번(Normal)으로 들어가서 기본 Idle이 됨
        Controller.StartActionBehavior(_currentType);
    }

    public override void Update()
    {
        // 3. 행동별 업데이트 로직
        switch (_currentType)
        {
            // 소음 유발자들 (주기적 신고)
            case PrisonerAIType.Singing:
            case PrisonerAIType.Screaming:
                _noiseTimer += Time.deltaTime;
                if (_noiseTimer > 3.0f)
                {
                    // 예: PrisonManager.Instance.ReportNoise(Controller.transform.position);
                    _noiseTimer = 0f;
                }
                break;

            // 7일차 기습 (거리 감지)
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

    public override void Exit()
    {
        // 4. 정리 (애니메이션 복구, 소리 끄기 등)
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 행동 중 맞았을 때 반응
        if (IsAggressiveType(_currentType))
        {
            fsm.ChangeState(fsm.CombatState); // 반격
        }
        else
        {
            fsm.ChangeState(fsm.CowerState); // 쫄음
        }
    }

    // 반격할 성격인지 판별
    private bool IsAggressiveType(PrisonerAIType type)
    {
        return type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.Escaper ||
               type == PrisonerAIType.Bad; // Bad 성향도 포함 가능
    }
}