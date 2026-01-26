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

        if (_currentType == PrisonerAIType.Good && Controller != null)
            _currentType = Controller.AIType;

        if (IsNormalIdleType(_currentType))
        {
            anim.SetBool("IsAction", false);
            int randomVariant = Random.Range(0, 3);
            anim.SetFloat("IdleVariant", randomVariant);
            StopMovement();
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
        if (IsNoisyType(_currentType))
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer > 3.0f)
            {
                _noiseTimer = 0f;
            }
        }

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
        // ★ [수정] 여기서 직접 Trigger를 켜지 않고, 각 상태(Combat/Cower)의 Enter()에 위임
        // 이렇게 해야 상태 전환과 애니메이션 실행 타이밍이 충돌하지 않습니다.

        if (IsAggressiveType(_currentType))
        {
            Debug.Log($"[{Controller.name}] 공격받음! 반격 시작.");
            fsm.ChangeState(fsm.CombatState);

            // CombatState 진입 후 즉시 피격 반응 처리를 위해 호출
            fsm.CombatState.OnDamaged(damage, hitPoint, hitDir);
        }
        else
        {
            Debug.Log($"[{Controller.name}] 공격받음! 겁먹음.");
            fsm.ChangeState(fsm.CowerState);
            // CowerState는 Enter()에서 애니메이션을 자동 실행하므로 추가 호출 불필요
        }
    }

    // ... (Helper Methods 등 나머지 코드는 기존과 동일) ...
    private void StartActionBehavior()
    {
        StopMovement();
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
        return type == PrisonerAIType.Good || type == PrisonerAIType.Bad;
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