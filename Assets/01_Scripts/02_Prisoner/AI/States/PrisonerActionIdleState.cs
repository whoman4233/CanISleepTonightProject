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

        // FSM에서 설정된 타입이 있다면 가져오기 (안전장치)
        if (_currentType == PrisonerAIType.Good && Controller != null)
            _currentType = Controller.AIType;

        // ================================================================
        // 1. 타입에 따른 행동 분기 (일반 vs 특수)
        // ================================================================
        if (IsNormalIdleType(_currentType))
        {
            // [일반 대기] Good, Bad 등
            anim.SetBool("IsAction", false);

            // -> 랜덤 대기 모션 (0 ~ 3번 중 하나) 선택
            int randomVariant = Random.Range(0, 4);
            anim.SetInteger("IdleVariant", randomVariant);

            // 이동 정지
            StopMovement();
        }
        else
        {
            // [특수 행동] 노래, 망치질, 기습 등
            anim.SetBool("IsAction", true);

            // 행동 시작 (소리 및 애니메이션)
            StartActionBehavior();

            // 로그 출력 (콘솔 확인용)
            PrintFlavorLog();
        }
    }

    public override void Update()
    {
        // ================================================================
        // 2. 특수 로직 (소음 및 기습) - 일반 죄수는 해당 없음
        // ================================================================

        // 소음 유발 로직 (3초마다)
        if (IsNoisyType(_currentType))
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer > 3.0f)
            {
                _noiseTimer = 0f;
                // 필요 시: PrisonManager.Instance.ReportNoise(...);
                // Debug.Log($"[{Controller.name}] 시끄러운 소리 내는 중...");
            }
        }

        // 기습(Ambusher) 로직
        if (_currentType == PrisonerAIType.Ambusher)
        {
            // 플레이어 감지 (3.5m 이내)
            if (player != null && Vector3.Distance(Controller.transform.position, player.position) < 3.5f)
            {
                Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} 기습 시작!</color>");

                // 문 강제 개방 및 전투 돌입
                PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
                fsm.ChangeState(fsm.CombatState);
            }
        }
    }

    public override void Exit()
    {
        // ★ 상태를 나갈 때(전투 돌입, 사망, 점호 등) 무조건 행동 및 소리를 끈다
        anim.SetBool("IsAction", false);
        Controller.StopActionBehavior();

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 애니메이션
        if (fsm != null && fsm.Anim != null)
        {
            fsm.Anim.SetTrigger("Hit");
        }

        // 성향에 따른 도주/전투 분기
        if (IsAggressiveType(_currentType))
        {
            Debug.Log($"[{Controller.name}] 공격받음! 반격 시작.");
            anim.SetTrigger("Hit");
            fsm.ChangeState(fsm.CombatState);
        }
        else
        {
            Debug.Log($"[{Controller.name}] 공격받음! 겁먹음.");
            anim.SetTrigger("HitCower");
            fsm.ChangeState(fsm.CowerState);
        }
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    private void StartActionBehavior()
    {
        StopMovement();
        // Controller를 통해 소리(SFX)와 애니메이션(ActionType 파라미터) 재생
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

    // 일반 Idle 모션을 재생해야 하는 타입인지 판별
    private bool IsNormalIdleType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Good ||
               type == PrisonerAIType.Bad;
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
               type == PrisonerAIType.Attacking; // Attacking 타입 추가
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