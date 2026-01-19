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

        var entry = inspectSfxTable != null
            ? inspectSfxTable.GetEntry(inspectObjectType)
            : null;

        if (entry != null && entry.animationSfx != null)
        {
            AudioManager.Instance.PlaySFX(entry.animationSfx);
        }

        if (animator != null)
            animator.SetTrigger(triggerName);

        if (outerCollider != null) //애니메이션 중 중복 클릭 방지
            outerCollider.enabled = false;
    }

    // Animation Event에서 호출
    public void AE_OnRevealCompleted()
    {
        var entry = inspectSfxTable != null
            ? inspectSfxTable.GetEntry(inspectObjectType)
            : null;

        if (entry != null && entry.discoverySfx != null)
        {
            AudioManager.Instance.PlaySFX(entry.discoverySfx);
        }

        foreach (var target in revealedTargets)
        {
            if (target != null)
                target.MarkRevealed();
        }

        foreach (var col in innerColliders)
        {
            if (col != null)
                col.enabled = true;
        }
    }
}
