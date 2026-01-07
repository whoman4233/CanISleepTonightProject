using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    private float triggerDistance = 3.5f; // 감지 거리
    private bool hasTriggered = false;

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm)
    {
    }

    public override void Enter()
    {
        base.Enter();
        hasTriggered = false;

        // 1. 이동 정지 & 대기 (숨어있는 연출)
        if (Agent != null) Agent.isStopped = true;

        // 애니메이션: 문 뒤에 서 있거나 숨어있는 모션
        if (Anim != null) Anim.CrossFade("Idle", 0.1f);
    }

    public override void Update()
    {
        if (hasTriggered || player == null) return;

        // 2. 플레이어 거리 체크
        float dist = Vector3.Distance(Controller.transform.position, player.position);

        if (dist <= triggerDistance)
        {
            TriggerAmbush();
        }
    }

    private void TriggerAmbush()
    {
        hasTriggered = true;
        Debug.Log($"[Ambush] {Controller.Data.ID}번 죄수 급습 시작!");

        // ★ [핵심 수정] 문 강제 개방 요청 (EventBus 사용)
        // 직접 참조(AssignedCell.cellDoor)가 없어도 ID만으로 문을 열 수 있음
        if (Controller.Data != null)
        {
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);
        }

        // 3. 전투 상태로 전환 (소리 지르며 뛰쳐나감)
        fsm.ChangeState(fsm.CombatState);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 숨어있다가 맞으면 즉시 기습 발동
        if (!hasTriggered)
        {
            TriggerAmbush();
        }
        else
        {
            fsm.ChangeState(fsm.CombatState);
        }
    }
}