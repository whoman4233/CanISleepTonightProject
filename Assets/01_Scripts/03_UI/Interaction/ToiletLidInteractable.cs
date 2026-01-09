using UnityEngine;

public class ToiletLidInteractable : MonoBehaviour, IInteractable
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string openTriggerName = "Open";

    [Header("Collider Control")]
    [SerializeField] private Collider outerCollider;

    private int _openTriggerHash;
    private bool _used;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _openTriggerHash = Animator.StringToHash(openTriggerName);
    }

    public void Interact(Player player)
    {
        if (_used)
            return;

        _used = true;

        animator.SetTrigger(_openTriggerHash);

        // 변기 재상호작용 차단
        if (outerCollider != null)
            outerCollider.enabled = false;
    }
}

