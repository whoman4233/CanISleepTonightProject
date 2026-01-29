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

        // ================================================================
        // ★ [핵심 수정] 상태 진입 시 이동 애니메이션 강제 종료
        // ================================================================
        // ReturnState나 다른 상태에서 Walk가 true인 채로 넘어왔을 경우,
        // 여기서 끄지 않으면 제자리에서 계속 걷는 모션이 나옵니다.
        anim.SetBool("Walk", false);
        anim.SetBool("Run", false);

        // 물리적인 이동 정지 (NavMeshAgent 멈춤)
        StopMovement();

        // 타입 초기화 (Good 타입으로 시작했어도 실제 컨트롤러 설정을 따라감)
        if (_currentType == PrisonerAIType.Good && Controller != null)
            _currentType = Controller.AIType;

        // 타입별 행동 분기
        if (IsNormalIdleType(_currentType))
        {
            anim.SetBool("IsAction", false);

            // 랜덤한 Idle 모션 재생 (0~2)
            int randomVariant = Random.Range(0, 3);
            anim.SetFloat("IdleVariant", randomVariant);
        }
        else
        {
            anim.SetBool("IsAction", true);
            StartActionBehavior();
            PrintFlavorLog();
        }
    }

    public override void Update()
    {
        // 1. 소음 유발 타입 (주기적 로그/소리)
        if (IsNoisyType(_currentType))
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer > 3.0f)
            {
                _noiseTimer = 0f;
                // 필요 시 여기서 사운드 재생 로직 추가 가능
            }
        }

        // 2. 기습(Ambush) 타입: 플레이어가 가까오면 전투로 전환
        if (_currentType == PrisonerAIType.Ambusher)
        {
            if (player != null && Vector3.Distance(Controller.transform.position, player.position) < 3.5f)
            {
                Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} 기습 시작!</color>");
                PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
                fsm.ChangeState(fsm.CombatState);
            }
        }
    }

    public override void Exit()
    {
        anim.SetBool("IsAction", false);
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 공격 성향에 따른 피격 반응 분기
        if (IsAggressiveType(_currentType))
        {
            Debug.Log($"[{Controller.name}] 공격받음! 반격 시작.");
            fsm.ChangeState(fsm.CombatState);

            // CombatState로 전환 후, 피격 정보 전달하여 즉각 반응 유도
            fsm.CombatState.OnDamaged(damage, hitPoint, hitDir);
        }
        else
        {
            Debug.Log($"[{Controller.name}] 공격받음! 겁먹음.");
            fsm.ChangeState(fsm.CowerState);
            // CowerState는 Enter()에서 웅크리기 애니메이션이 자동 실행됨
        }
    }

    // ... (Helper Methods) ...

    private void StartActionBehavior()
    {
        StopMovement(); // 행동 시작 전 확실히 멈춤
        Controller.StartActionBehavior(_currentType);
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private bool IsNormalIdleType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Good ||
               type == PrisonerAIType.Bad ||
               type == PrisonerAIType.QTE_Attacker ||
               type == PrisonerAIType.Escaper;
    }

    private bool IsNoisyType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Singing ||
               type == PrisonerAIType.Screaming ||
               type == PrisonerAIType.HammeringWall;
    }

    private bool IsAggressiveType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Bad ||
               type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Escaper ||
               type == PrisonerAIType.Attacking;
    }

    // [로그 출력용 함수]
    private void PrintFlavorLog()
    {
        string id = Controller.Data != null ? Controller.Data.ID : Controller.name;

        switch (_currentType)
        {
            case PrisonerAIType.Crying:
                Debug.Log($"<color=cyan>[{id}] 흑흑.. 잘못했어요.. (우는 소리 재생 중)</color>");
                break;
            case PrisonerAIType.Singing:
                Debug.Log($"<color=yellow>[{id}] 랄라라~ 콧노래 부르는 중 (노래 소리 재생 중)</color>");
                break;
            case PrisonerAIType.Mumbling:
                Debug.Log($"<color=grey>[{id}] 중얼중얼.. 벽보고 이야기 중 (중얼거림 재생 중)</color>");
                break;
            case PrisonerAIType.HammeringWall:
                Debug.Log($"<color=red>[{id}] 쾅! 쾅! 벽을 부수는 중 (망치 소리 재생 중)</color>");
                break;
            case PrisonerAIType.Screaming:
                Debug.Log($"<color=red>[{id}] 으아아아악!! (비명 지르는 중)</color>");
                break;
            case PrisonerAIType.Deadlift:
                Debug.Log($"<color=green>[{id}] 흡! 합! (운동 중)</color>");
                break;
            case PrisonerAIType.Ambusher:
                Debug.Log($"<color=red>[{id}] (문 뒤에서 숨 죽이는 중...)</color>");
                break;
        }
    }
}