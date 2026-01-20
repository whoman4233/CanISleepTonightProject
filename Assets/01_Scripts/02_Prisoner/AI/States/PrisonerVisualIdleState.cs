using UnityEngine;

public class PrisonerVisualIdleState : IPrisonerState
{
    private PrisonerFSM _fsm;

    public PrisonerVisualIdleState(PrisonerFSM fsm)
    {
        _fsm = fsm;
    }

    public void Enter()
    {
        if (_fsm.Controller.AssignedCell == null) return;

        // 1. ScheduleManager에서 현재 내 역할(VisualType) 가져오기
        var dailyRole = PrisonerScheduleManager.Instance.GetDailyRole(_fsm.Controller.AssignedCell.cellId);
        VisualAnomalyType myVisual = dailyRole.visualType;

        Debug.Log($"[VisualIdle] 진입: {myVisual}");

        // 2. 기본 파라미터 설정 (메인 분기용)
        _fsm.Anim.SetFloat("VisualIdleType", (float)myVisual);
        _fsm.Anim.SetBool("IsVisualIdle", true);

        // ================================================================
        // ★ [추가] Frank 계열인 경우, 2개의 모션 중 하나를 랜덤 선택
        // ================================================================
        if (IsFrankType(myVisual))
        {
            // 0 또는 1을 랜덤으로 뽑음 (int형 파라미터 필요)
            int randomVariant = Random.Range(0, 2);
            _fsm.Anim.SetInteger("VisualIdleVariant", randomVariant);

            Debug.Log($"[VisualIdle] Frank 랜덤 모션 선택: {randomVariant}번");
        }
        else
        {
            // Frank가 아니면 기본값 0
            _fsm.Anim.SetInteger("VisualIdleVariant", 0);
        }
    }

    public void Update()
    {
        // 로직 없음 (애니메이션만 재생)
    }

    public void Exit()
    {
        // 나갈 때 Bool 끄기
        _fsm.Anim.SetBool("IsVisualIdle", false);
    }

    public void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 시 처리 (기존과 동일)
        _fsm.Anim.SetTrigger("Hit");
        _fsm.Anim.SetBool("IsVisualIdle", false); // 확실하게 끄기

        PrisonerAIType aiType = _fsm.Controller.AIType;

        if (aiType == PrisonerAIType.Good || aiType == PrisonerAIType.Crying)
        {
            if (_fsm.CowerState != null) _fsm.ChangeState(_fsm.CowerState);
            else _fsm.ChangeState(_fsm.ActionState);
        }
        else
        {
            if (_fsm.CombatState != null) _fsm.ChangeState(_fsm.CombatState);
        }
    }

    public void OnStartInspection()
    {
        // 문 열리면(점호) 특수 상태 혹은 기본 점호 상태로 전환
        _fsm.ChangeState(_fsm.InspectionState);
    }

    // 헬퍼: Frank 타입인지 확인
    private bool IsFrankType(VisualAnomalyType type)
    {
        return type == VisualAnomalyType.PSN_FrankeA ||
               type == VisualAnomalyType.PSN_FrankeB ||
               type == VisualAnomalyType.PSN_FrankeR;
    }
}