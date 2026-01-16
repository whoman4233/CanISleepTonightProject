using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    // 기습 감지 거리 (문 뒤에서 플레이어 감지)
    private const float AmbushDistance = 3.5f;

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter(); // Base에 초기화 로직이 있다면 실행

        // [수정 1] 플레이어 찾기 안전장치
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
            else Debug.LogWarning($"[AmbushState] {Controller.name}: 플레이어를 찾을 수 없습니다! Tag를 확인하세요.");
        }

        // [수정 2] 이동 멈춤 확실하게 처리
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath(); // 경로 초기화
        }

        // [수정 3] Controller를 통해 애니메이션(ID 9) 및 소리 실행
        // (직접 SetBool 하지 말고 통합 메서드 사용 권장)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        Debug.Log($"[Ambush] {Controller.name} 기습 대기 시작. (거리: {AmbushDistance})");
    }

    public override void Update()
    {
        // 플레이어 없으면 재탐색 시도
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
            return;
        }

        // 1. 거리 계산
        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // [디버깅] 거리 실시간 확인 (필요 시 주석 해제)
        // Debug.Log($"Distance to Player: {dist}");

        // 2. 사거리 안에 들어오면 기습 시작
        if (dist <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 놈이 왔다! 기습 개시!</color>");

            // 문을 강제로 엶
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            // [중요] 상태 전환 전 행동 종료 (루프 소리 끄기 등)
            Controller.StopActionBehavior();

            // 전투 상태로 전환
            fsm.ChangeState(fsm.CombatState);
        }
    }

    public override void Exit()
    {
        // 상태 나갈 때 행동 정리
        Controller.StopActionBehavior();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Debug.Log($"[Ambush] {Controller.name} : 숨어있다가 걸림! 전투 돌입.");

        // 행동 종료 후 전투 전환
        Controller.StopActionBehavior();
        fsm.ChangeState(fsm.CombatState);
    }
}