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

        // ================================================================
        // ★ [추가] 용의자(Suspect) 그룹 예외 처리 -> Action 12번 실행
        // ================================================================
        if (IsSuspectType(myVisual))
        {
            Debug.Log($"[VisualIdle] 용의자({myVisual}) 감지 -> Action 12번 강제 실행");

            // Controller에 추가한 StartActionBehavior(int) 함수를 호출
            // (주의: PrisonerController에 int 오버로딩 함수가 있어야 합니다)
            _fsm.Controller.StartActionBehavior(12);

            // ★ 여기서 return하여 아래의 일반 VisualIdle 로직(IsVisualIdle 켜기 등)을 실행하지 않음
            return;
        }

        // ================================================================
        // 2. 일반 VisualAnomaly (Frank, Bikini 등) 처리
        // ================================================================

        // 기본 파라미터 설정 (메인 분기용)
        _fsm.Anim.SetFloat("VisualIdleType", (float)myVisual);
        _fsm.Anim.SetBool("IsVisualIdle", true);

        // Frank 계열인 경우, 랜덤 모션 선택
        if (IsFrankType(myVisual))
        {
            // 0 또는 1을 랜덤으로 뽑음
            int randomVariant = Random.Range(0, 2);
            _fsm.Anim.SetInteger("VisualIdleVariant", randomVariant);
            Debug.Log($"[VisualIdle] Frank 랜덤 모션 선택: {randomVariant}번");
        }
        else
        {
            _fsm.Anim.SetInteger("VisualIdleVariant", 0);
        }
    }

    public void Update()
    {
        // 로직 없음 (애니메이션만 재생)
    }

    public void Exit()
    {
        // 1. VisualIdle 끄기
        _fsm.Anim.SetBool("IsVisualIdle", false);

        // ★ [추가] Suspect였을 경우 켜진 ActionBehavior 끄기 (안전장치)
        _fsm.Controller.StopActionBehavior();
    }

    public void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 시 처리
        _fsm.Anim.SetTrigger("Hit");
        _fsm.Anim.SetBool("IsVisualIdle", false);
        _fsm.Controller.StopActionBehavior(); // ★ [추가] 피격 시 행동 중단

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

    // ★ [추가] 헬퍼: Suspect 타입인지 확인
    private bool IsSuspectType(VisualAnomalyType type)
    {
        return type == VisualAnomalyType.Suspect1 ||
               type == VisualAnomalyType.Suspect2 ||
               type == VisualAnomalyType.Suspect3;
    }
}