using UnityEngine;

public class PrisonerActionIdleState : BasePrisonerState
{
    private PrisonerAIType _currentType;
    private float _noiseTimer = 0f;

    public PrisonerActionIdleState(PrisonerFSM fsm) : base(fsm) { }

    // FSM에서 "너 이번엔 노래 불러(Singing)"라고 알려주는 함수
    public void SetActionType(PrisonerAIType aiType)
    {
        _currentType = aiType;
    }

    public override void Enter()
    {
        base.Enter();

        // 1. 이동 완전 정지 (제자리 행동이므로)
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // 2. 컨트롤러에게 행동 개시 명령 (애니메이션 전환, 소리 재생, 프롭 들기)
        Controller.StartActionBehavior(_currentType);
    }

    public override void Update()
    {
        // 3. 주기적 소음 발생 로직 (노래, 비명 등)
        if (_currentType == PrisonerAIType.Singing || _currentType == PrisonerAIType.Screaming)
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer > 3.0f)
            {
                // 게임 매니저에 소음 신고 (필요 시 주석 해제)
                // PrisonManager.Instance.ReportNoise(Controller.transform.position);
                _noiseTimer = 0f;
            }
        }

        // 4. [7일차] 기습(Ambush) 감지 로직
        if (_currentType == PrisonerAIType.Ambusher)
        {
            CheckAmbushTrigger();
        }
    }

    public override void Exit()
    {
        // 5. 나갈 때 정리 (애니메이션 복구, 소리 끄기, 도구 넣기)
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 행동 중 맞았을 때 반응 분기
        // 무기를 든 행동(망치)이나 공격적인 성향(기습, 탈옥)은 반격
        if (_currentType == PrisonerAIType.HammeringWall ||
            _currentType == PrisonerAIType.Ambusher ||
            _currentType == PrisonerAIType.Escaper)
        {
            fsm.ChangeState(fsm.CombatState);
        }
        else
        {
            // 나머지는 쫄아서 웅크림
            fsm.ChangeState(fsm.CowerState);
        }
    }

    // 7일차 기습 트리거 체크
    private void CheckAmbushTrigger()
    {
        if (player != null && Vector3.Distance(Controller.transform.position, player.position) < 3.5f)
        {
            // 문 열고 뛰쳐나감
            if (Controller.Data != null) PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
            fsm.ChangeState(fsm.CombatState);
        }
    }
}