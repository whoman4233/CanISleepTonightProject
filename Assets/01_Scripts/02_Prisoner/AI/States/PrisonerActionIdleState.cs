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

        // 1. 행동 시작 (소리 및 애니메이션)
        StartActionBehavior();

        // 2. ★ [추가됨] 여기서 로그를 찍어야 콘솔에 나옵니다!
        PrintFlavorLog();
    }

    public override void Update()
    {
        // 소음 유발 로직 (3초마다)
        if (IsNoisyType(_currentType))
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer > 3.0f)
            {
                _noiseTimer = 0f;
                // 필요 시: PrisonManager.Instance.ReportNoise(...);
            }
        }

        // 기습(Ambusher) 로직
        if (_currentType == PrisonerAIType.Ambusher)
        {
            if (player != null && Vector3.Distance(Controller.transform.position, player.position) < 3.5f)
            {
                Debug.Log($"[Ambush] {Controller.Data.ID} 기습 시작!");
                PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
                fsm.ChangeState(fsm.CombatState);
            }
        }
    }

    private void StartActionBehavior()
    {
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
        // Controller를 통해 소리(SFX)와 애니메이션 재생
        Controller.StartActionBehavior(_currentType);
    }

    public override void Exit()
    {
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (IsAggressiveType(_currentType))
            fsm.ChangeState(fsm.CombatState);
        else
            fsm.ChangeState(fsm.CowerState);
    }

    // ================================================================
    // [로그 출력용 함수]
    // ================================================================
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
        }
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
               type == PrisonerAIType.Escaper;
    }
}