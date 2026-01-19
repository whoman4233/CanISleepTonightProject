using UnityEngine;

public class InspectAnimatedRevealAction : MonoBehaviour, IInspectAction
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Inspect";

    [Header("Inspect Identity")]
    [SerializeField] private InspectObjectType inspectObjectType;
    [SerializeField] private InspectSfxTableSO inspectSfxTable;

    [Header("Collider Control")]
    [SerializeField] private Collider outerCollider;
    [SerializeField] private Collider[] innerColliders;

    [Header("아웃라이너 보여줄 대상(숨긴물건)")]
    [SerializeField] private InspectTarget[] revealedTargets;

    private bool _used;

    public void InspectAction(IInspectable owner)
    {
        if (_used)
            return;

        _used = true;

        // Inspect 애니메이션 시작 SFX
        if (inspectSfxTable != null)
        {
            AudioClip animationSfx =
                inspectSfxTable.GetAnimationSfx(inspectObjectType);

            if (animationSfx != null)
            {
                AudioManager.Instance.PlaySFX(animationSfx);
            }
        }

        // 애니메이션 트리거
        if (animator != null)
            animator.SetTrigger(triggerName);

        // 애니메이션 중 중복 클릭 방지
        if (outerCollider != null)
            outerCollider.enabled = false;
    }

    // Animation Event에서 호출
    public void AE_OnRevealCompleted()
    {
        // Inspect 대상 Reveal (연출)
        foreach (var target in revealedTargets)
        {
            if (target == null)
                continue;

            target.MarkRevealed();
        }

        // 내부 콜라이더 활성화
        foreach (var col in innerColliders)
        {
            if (col != null)
                col.enabled = true;
        }
    }
}


