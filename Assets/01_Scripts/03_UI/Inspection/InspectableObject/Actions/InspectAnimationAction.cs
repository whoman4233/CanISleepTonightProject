using UnityEngine;

public class InspectAnimatedRevealAction : MonoBehaviour, IInspectAction
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Inspect";

    [Header("Collider Control")]
    [SerializeField] private Collider outerCollider;
    [SerializeField] private Collider[] innerColliders;

    private bool _used;
    public void InspectAction(IInspectable owner)
    {
        if (_used)
            return;

        _used = true;

        if (animator != null)
            animator.SetTrigger(triggerName);

        // 애니메이션 중 중복 클릭 방지
        if (outerCollider != null)
            outerCollider.enabled = false;
    }

    // Animation Event에서 호출
    public void AE_OnRevealCompleted()
    {
        foreach (var col in innerColliders)
        {
            if (col != null)
                col.enabled = true;
        }
    }
}
