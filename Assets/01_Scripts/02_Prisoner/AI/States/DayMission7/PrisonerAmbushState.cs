using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    // 기습 감지 거리 (문 뒤에서 플레이어 감지)
    private const float AmbushDistance = 3.5f;
    // 목적지 도착 판정 거리
    private const float ArrivalDistance = 0.5f;

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter(); // Base에 초기화 로직이 있다면 실행

        // 1. 플레이어 찾기 안전장치
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
            else Debug.LogWarning($"[AmbushState] {Controller.name}: 플레이어를 찾을 수 없습니다! Tag를 확인하세요.");
        }

        // ================================================================
        // [수정 핵심] 멈추지 않고 InspectionPoint로 이동 명령
        // ================================================================
        if (agent != null && agent.isOnNavMesh)
        {
            if (fsm.InspectionPoint != null)
            {
                agent.isStopped = false; // 이동 허용
                agent.SetDestination(fsm.InspectionPoint.position); // 목적지 설정
                Debug.Log($"[Ambush] {Controller.name} -> InspectionPoint로 이동 시작.");
            }
            else
            {
                // InspectionPoint가 없으면 제자리 대기
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        // 기습 행동(애니메이션) 시작 
        // (주의: 이동 모션이 아니라 기습 대기 모션(ID 9)이 재생되면서 슬라이딩할 수 있음. 
        //  만약 이동 모션이 필요하다면 도착 후 호출하도록 Update로 옮겨야 함)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        Debug.Log($"[Ambush] {Controller.name} 기습 대기 시작. (감지 거리: {AmbushDistance})");
    }

    public override void Update()
    {
        // 플레이어 재탐색
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
            return;
        }

        // 1. 플레이어와의 거리 계산 (기습 조건)
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);

        // 2. 사거리 안에 들어오면 즉시 기습 시작 (이동 중이라도 캔슬하고 덤빔)
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 놈이 왔다! 기습 개시!</color>");

            // 문 강제 개방
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            // 행동 종료 및 전투 전환
            Controller.StopActionBehavior();
            fsm.ChangeState(fsm.CombatState);
            return;
        }

        // 3. [추가] InspectionPoint 도착 체크
        // (도착했으면 멈춰서서 대기)
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance && !agent.pathPending)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                Debug.Log($"[Ambush] {Controller.name} 매복 위치 도착 완료. 대기 중...");
            }
        }
    }

    public override void Exit()
    {
        // 상태 나갈 때 행동 정리
        Controller.StopActionBehavior();

        // 이동 중이었을 수 있으므로 멈춤 처리
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Debug.Log($"[Ambush] {Controller.name} : 숨어있다가 걸림! 전투 돌입.");

        Controller.StopActionBehavior();
        fsm.ChangeState(fsm.CombatState);
    }
}