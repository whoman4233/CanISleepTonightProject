using UnityEngine;

public class PrisonerEscapeState : BasePrisonerState
{
    // 탈출 목적지 (없으면 임시로 0,0,0)
    private Vector3 _escapeDestination = Vector3.zero;

    public PrisonerEscapeState(PrisonerFSM fsm) : base(fsm)
    {
        // 게임 내 "EscapePoint"라는 이름의 오브젝트가 있다면 그 위치를 찾음 (없으면 0,0,0)
        var exitObj = GameObject.Find("EscapePoint");
        if (exitObj != null) _escapeDestination = exitObj.transform.position;
    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log($"[AI] {Controller.name}: 자유다!! (탈주 시도)");

        // 1. 문 열기 시도 (내 방에 배정된 문이 있다면)
        if (Controller.AssignedCell != null)
        {
            // 강제로 문을 여는 이벤트 발생 (혹은 직접 호출)
            // 여기서는 간단하게 "문 열어!" 이벤트 발행
            PrisonerEventBus.PublishForceOpenDoor(Controller.AssignedCell.cellId);
        }

        // 2. 애니메이션 설정 (파라미터 이름 수정: IsRun -> Run)
        Anim.SetBool("Walk", false);
        Anim.SetBool("Run", true); // ★ 수정됨

        // 3. 이동 로직
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = false;
            Agent.speed = 4.0f; // 조금 더 빠르게
            Agent.SetDestination(_escapeDestination); // ★ 수정됨: 실제 탈출구로 이동
        }
    }

    public override void Update()
    {
        // (선택) 만약 목적지에 도착했다면? -> 게임에서 사라지게 하거나 승리 처리
        if (Agent != null && Agent.remainingDistance < 1.0f && !Agent.pathPending)
        {
            // 탈출 성공 처리 (예: 사라짐)
            Debug.Log($"[AI] {Controller.name}: 탈출 성공! (Destroy)");
            // Controller.gameObject.SetActive(false); // 일단 숨김 처리
        }
    }

    public override void Exit()
    {
        Anim.SetBool("Run", false); // ★ 수정됨
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.ResetPath();
            Agent.speed = 2.0f; // 속도 원복
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 맞으면 전투 태세로 전환
        fsm.ChangeState(fsm.CombatState);
    }
}