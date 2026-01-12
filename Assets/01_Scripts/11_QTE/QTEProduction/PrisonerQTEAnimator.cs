using UnityEngine;

public class PrisonerQTEAnimator : MonoBehaviour
{
    [Header("Optional Override")]
    [SerializeField] private Animator animator;

    [Header("Animator Triggers")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string failTriggerName = "Hit";

    private int _attackHash;
    private int _failHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _attackHash = Animator.StringToHash(attackTriggerName);
        _failHash = Animator.StringToHash(failTriggerName);
    }

    public void PlayAttack()
    {
        if (animator == null) return;
        animator.ResetTrigger(_failHash);
        animator.SetTrigger(_attackHash);
    }

    public void PlayFail()
    {
        if (animator == null) return;
        animator.ResetTrigger(_attackHash);
        animator.SetTrigger(_failHash);
    }
}
