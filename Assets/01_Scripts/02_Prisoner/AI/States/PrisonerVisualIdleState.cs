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
        // ★ [추가] GoatHead 타입 예외 처리 -> Action 13번 실행
        // ================================================================
        if (IsGoatHeadType(myVisual))
        {
            Debug.Log($"[VisualIdle] GoatHead({myVisual}) 감지 -> Action 13번 실행 (IsAction On)");
            _fsm.Controller.StartActionBehavior(12);
            _fsm.Anim.SetBool("IsAction", true);
            return;
        }

        // ================================================================
        // 2. 용의자(Suspect) 그룹 예외 처리 -> Action 12번 실행
        // ================================================================
        if (IsSuspectType(myVisual))
        {
            Debug.Log($"[VisualIdle] 용의자({myVisual}) 감지 -> Action 12번 강제 실행");
            _fsm.Controller.StartActionBehavior(PrisonerAIType.Suss);
            return;
        }

        // ================================================================
        // 3. 일반 VisualAnomaly (Frank, Bikini 등) 처리
        // ================================================================

        _fsm.Anim.SetFloat("VisualIdleType", (float)myVisual);
        _fsm.Anim.SetBool("IsVisualIdle", true);

        if (IsFrankType(myVisual))
        {
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
        _fsm.Anim.SetBool("IsAction", false);

        // 2. ActionBehavior 끄기
        _fsm.Controller.StopActionBehavior();
    }

    public void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // ★ [핵심 수정] 용의자(Suss)는 맞아도 가만히 있어야 함
        // 현재 상태가 용의자인지 다시 확인
        if (_fsm.Controller.AssignedCell != null)
        {
            var dailyRole = PrisonerScheduleManager.Instance.GetDailyRole(_fsm.Controller.AssignedCell.cellId);
            if (IsSuspectType(dailyRole.visualType))
            {
                // 피격 애니메이션(움찔)은 재생하되,
                _fsm.Anim.SetTrigger("Hit");

                // 행동(Action)은 끄지 않고, CombatState로 전환도 하지 않음
                Debug.Log($"[VisualIdle] 용의자 피격 -> Combat 전환 방지");
                return;
            }
        }

        // --- 일반 죄수 로직 (반격) ---

        // 피격 시 처리
        _fsm.Anim.SetTrigger("Hit");
        _fsm.Anim.SetBool("IsVisualIdle", false);

        // 피격 시 IsAction 해제
        _fsm.Anim.SetBool("IsAction", false);

        _fsm.Controller.StopActionBehavior(); // 행동 중단

        PrisonerAIType aiType = _fsm.Controller.AIType;

        if (_fsm.CombatState != null) _fsm.ChangeState(_fsm.CombatState);
    }

    public void OnStartInspection()
    {
        var dailyRole = PrisonerScheduleManager.Instance.GetDailyRole(_fsm.Controller.AssignedCell.cellId);
        VisualAnomalyType myVisual = dailyRole.visualType;

        if (!IsGoatHeadType(myVisual))
        {
            _fsm.ChangeState(_fsm.InspectionState);
        }
        else
        {
            Debug.Log($"[VisualIdle] {myVisual}: 점호 시작 무시 (GoatHead Logic)");
            return;
        }
    }

    // 헬퍼: Frank 타입인지 확인
    private bool IsFrankType(VisualAnomalyType type)
    {
        return type == VisualAnomalyType.PSN_FrankeA ||
               type == VisualAnomalyType.PSN_FrankeB ||
               type == VisualAnomalyType.PSN_FrankeR;
    }

    // 헬퍼: Suspect 타입인지 확인
    private bool IsSuspectType(VisualAnomalyType type)
    {
        return type == VisualAnomalyType.Suspect1 ||
               type == VisualAnomalyType.Suspect2 ||
               type == VisualAnomalyType.Suspect3;
    }

    // 헬퍼: GoatHead 타입인지 확인
    private bool IsGoatHeadType(VisualAnomalyType type)
    {
        return type.ToString().Contains("GoatHead");
    }
}