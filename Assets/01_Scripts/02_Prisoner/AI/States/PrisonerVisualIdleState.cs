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

            // 1. ActionType 13번 설정 (Controller의 int 오버로딩 활용)
            _fsm.Controller.StartActionBehavior(12);

            // 2. IsAction True 설정 (Controller 함수는 파라미터만 세팅하고 IsAction을 안 켤 수도 있으므로 수동 설정)
            _fsm.Anim.SetBool("IsAction", true);

            // ★ 여기서 return하여 아래의 일반 VisualIdle 로직을 실행하지 않음
            return;
        }

        // ================================================================
        // 2. 용의자(Suspect) 그룹 예외 처리 -> Action 12번 실행
        // ================================================================
        if (IsSuspectType(myVisual))
        {
            Debug.Log($"[VisualIdle] 용의자({myVisual}) 감지 -> Action 12번 강제 실행");

            // Controller에 추가한 StartActionBehavior(int) 함수를 호출
            _fsm.Controller.StartActionBehavior(PrisonerAIType.Suss);

            return;
        }

        // ================================================================
        // 3. 일반 VisualAnomaly (Frank, Bikini 등) 처리
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

        // ★ [추가] IsAction 끄기 (GoatHead 등에서 켜졌을 수 있으므로)
        _fsm.Anim.SetBool("IsAction", false);

        // 2. ActionBehavior 끄기
        _fsm.Controller.StopActionBehavior();
    }

    public void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 시 처리
        _fsm.Anim.SetTrigger("Hit");
        _fsm.Anim.SetBool("IsVisualIdle", false);

        // ★ [추가] 피격 시 IsAction 해제
        _fsm.Anim.SetBool("IsAction", false);

        _fsm.Controller.StopActionBehavior(); // 행동 중단

        PrisonerAIType aiType = _fsm.Controller.AIType;

        if (_fsm.CombatState != null) _fsm.ChangeState(_fsm.CombatState);
    }

    public void OnStartInspection()
    {
        var dailyRole = PrisonerScheduleManager.Instance.GetDailyRole(_fsm.Controller.AssignedCell.cellId);
        VisualAnomalyType myVisual = dailyRole.visualType;

        // [수정] 단순 비교(!=) 대신 헬퍼 메서드(!IsGoatHeadType) 사용
        // 염소 머리 타입이 '아닐 때만' 점호 상태로 전환
        if (!IsGoatHeadType(myVisual))
        {
            _fsm.ChangeState(_fsm.InspectionState);
        }
        else
        {
            // 염소 머리는 점호 때 아무것도 안 함 (제자리 유지)
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

    // ★ [추가] 헬퍼: GoatHead 타입인지 확인
    private bool IsGoatHeadType(VisualAnomalyType type)
    {
        // Enum 이름에 GoatHead가 포함되어 있으면 true (예: PSN_GoatHead)
        return type.ToString().Contains("GoatHead");
    }
}