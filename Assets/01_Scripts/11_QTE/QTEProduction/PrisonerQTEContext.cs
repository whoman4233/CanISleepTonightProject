using UnityEngine;

public static class PrisonerQTEContext
{
    public static GameObject CurrentAttacker { get; private set; }

    // 기존: Animator 참조 (필요하면 계속 사용)
    public static Animator CurrentAttackerAnimator { get; private set; }

    // ★ 추가: QTE 전용 애니메이터 래퍼(메서드 보유) 참조
    public static PrisonerQTEAnimator CurrentAttackerQTEAnimator { get; private set; }

    public static QTEActionSO CurrentAction { get; set; }
    public static QTEResult CurrentResult { get; set; }

    // 데미지 소비 여부 (중복 데미지 방지 핵심)
    public static bool DamageConsumed { get; set; }

    public static void SetAttacker(Transform attacker)
    {
        Debug.Log($"[QTEContext] SetAttacker called by {attacker?.name} | Frame={Time.frameCount}");

        if (attacker == null)
        {
            Clear();
            return;
        }

        CurrentAttacker = attacker.gameObject;

        // 기존 유지
        CurrentAttackerAnimator = attacker.GetComponentInChildren<Animator>();

        // ★ 핵심: QTE 전용 컴포넌트(메서드 호출용)
        CurrentAttackerQTEAnimator = attacker.GetComponentInChildren<PrisonerQTEAnimator>();

        // (선택) 혹시나 못 찾으면, 같은 오브젝트에서도 한번 더 탐색
        if (CurrentAttackerQTEAnimator == null)
            CurrentAttackerQTEAnimator = attacker.GetComponent<PrisonerQTEAnimator>();
    }

    public static void Clear()
    {
        CurrentAttacker = null;
        CurrentAttackerAnimator = null;
        CurrentAttackerQTEAnimator = null;

        CurrentAction = null;
        CurrentResult = default;

        DamageConsumed = false;
    }
}
