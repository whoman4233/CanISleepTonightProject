using UnityEngine;

public class PrisonerActionIdleState : BasePrisonerState
{
    private PrisonerAIType _currentType;
    private float _noiseTimer = 0f;
    private bool _isReturningToCell = false; // 복귀 중인지 체크

    public PrisonerActionIdleState(PrisonerFSM fsm) : base(fsm) { }

    // FSM 초기화 시 어떤 행동을 할지 설정
    public void SetActionType(PrisonerAIType aiType)
    {
        _currentType = aiType;
    }

    public override void Enter()
    {
        base.Enter();

        // ★ [복귀 로직 수정] 
        // 감방의 중앙(CellAnchor.transform)이 아니라, 죄수 스폰 위치(prisonerSpawn)를 기준점으로 잡습니다.
        Transform origin = null;
        if (Controller.AssignedCell != null)
        {
            origin = Controller.AssignedCell.prisonerSpawn;
        }

        // 기준점이 있고, 현재 위치가 그곳에서 멀리 떨어져 있다면(0.5m 이상) -> 복귀 모드 진입
        if (origin != null && Vector3.Distance(fsm.transform.position, origin.position) > 0.5f)
        {
            _isReturningToCell = true;
            agent.isStopped = false;
            agent.SetDestination(origin.position);
            anim.SetBool("Walk", true);
        }
        else
        {
            // 이미 제자리(또는 기준점 없음)라면 바로 행동 시작
            StartActionBehavior();
        }
    }

    public override void Update()
    {
        // 1. 복귀 중일 때의 로직
        if (_isReturningToCell)
        {
            // 도착 체크
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // 도착 완료
                _isReturningToCell = false;
                anim.SetBool("Walk", false);
                agent.isStopped = true;

                // 위치와 회전을 정확하게 맞추고 싶다면 여기서 강제 조정 가능
                // fsm.transform.position = Controller.AssignedCell.prisonerSpawn.position;
                // fsm.transform.rotation = Controller.AssignedCell.prisonerSpawn.rotation;

                // 제자리 행동 시작
                StartActionBehavior();
            }
            return; // 복귀 중에는 아래의 행동 로직(소음, 기습 등)을 실행하지 않음
        }

        // 2. 기존 행동 로직 (소음, 기습 감지 등)
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

    private void StartActionBehavior()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // 컨트롤러에게 행동 개시 명령 (애니메이션, 소리, 프롭)
        Controller.StartActionBehavior(_currentType);
    }

    public override void Exit()
    {
        // 행동 정리
        Controller.StopActionBehavior();
        anim.SetBool("Walk", false);
        _isReturningToCell = false;
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 행동 중 맞았을 때 반응 (복귀 중이든 아니든 맞으면 전투/쫄기)
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
               type == PrisonerAIType.Bad;
    }
}