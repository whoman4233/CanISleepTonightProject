using UnityEngine;

public class PrisonerVisualIdleState : IPrisonerState
{
    private PrisonerFSM _fsm;

    // ================================================================
    // Animator Hashes 캐싱
    // ================================================================
    private static readonly int IsActionHash = Animator.StringToHash("IsAction");
    private static readonly int VisualIdleTypeHash = Animator.StringToHash("VisualIdleType");
    private static readonly int IsVisualIdleHash = Animator.StringToHash("IsVisualIdle");
    private static readonly int VisualIdleVariantHash = Animator.StringToHash("VisualIdleVariant");
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");

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

        // GoatHead 타입 예외 처리 -> Action 13번 실행
        if (IsGoatHeadType(myVisual))
        {
            Debug.Log($"[VisualIdle] GoatHead({myVisual}) 감지 -> Action 13번 실행 (IsAction On)");
            _fsm.Controller.StartActionBehavior(12);
            // [수정] Hash 사용
            _fsm.Anim.SetBool(IsActionHash, true);
            return;
        }

        // 용의자(Suspect) 그룹 예외 처리 -> Action 12번 실행
        if (IsSuspectType(myVisual))
        {
            Debug.Log($"[VisualIdle] 용의자({myVisual}) 감지 -> Action 12번 강제 실행");
            _fsm.Controller.StartActionBehavior(PrisonerAIType.Suss);
            return;
        }

        // 일반 VisualAnomaly (Frank, Bikini 등) 처리
        // [수정] Hash 사용
        _fsm.Anim.SetFloat(VisualIdleTypeHash, (float)myVisual);
        _fsm.Anim.SetBool(IsVisualIdleHash, true);

        if (IsFrankType(myVisual))
        {
            int randomVariant = Random.Range(0, 2);
            // [수정] Hash 사용
            _fsm.Anim.SetInteger(VisualIdleVariantHash, randomVariant);
            Debug.Log($"[VisualIdle] Frank 랜덤 모션 선택: {randomVariant}번");
        }
        else
        {
            // [수정] Hash 사용
            _fsm.Anim.SetInteger(VisualIdleVariantHash, 0);
        }
    }

    public void Update() { }

    public void Exit()
    {
        // [수정] Hash 사용
        _fsm.Anim.SetBool(IsVisualIdleHash, false);
        _fsm.Anim.SetBool(IsActionHash, false);

        _fsm.Controller.StopActionBehavior();
    }

    public void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (_fsm.Controller.AssignedCell != null)
        {
            var dailyRole = PrisonerScheduleManager.Instance.GetDailyRole(_fsm.Controller.AssignedCell.cellId);
            if (IsSuspectType(dailyRole.visualType))
            {
                // [수정] Hash 사용
                _fsm.Anim.SetTrigger(HitTriggerHash);
                Debug.Log($"[VisualIdle] 용의자 피격 -> Combat 전환 방지");
                return;
            }
        }

        // --- 일반 죄수 로직 (반격) ---
        // [수정] Hash 사용
        _fsm.Anim.SetTrigger(HitTriggerHash);
        _fsm.Anim.SetBool(IsVisualIdleHash, false);
        _fsm.Anim.SetBool(IsActionHash, false);

        _fsm.Controller.StopActionBehavior();

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

    // 헬퍼 메서드들은 기존과 동일
    private bool IsFrankType(VisualAnomalyType type) { /* ... */ return type == VisualAnomalyType.PSN_FrankeA || type == VisualAnomalyType.PSN_FrankeB || type == VisualAnomalyType.PSN_FrankeR; }
    private bool IsSuspectType(VisualAnomalyType type) { /* ... */ return type == VisualAnomalyType.Suspect1 || type == VisualAnomalyType.Suspect2 || type == VisualAnomalyType.Suspect3; }
    private bool IsGoatHeadType(VisualAnomalyType type) { return type.ToString().Contains("GoatHead"); }
}