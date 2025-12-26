using UnityEngine;

public class InspectAnimationAction : MonoBehaviour, IInspectAction
{
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Inspect";

    public void InspectAction(IInspectable owner)
    {
        if (animator == null)
            return;

        animator.SetTrigger(triggerName);
    }
}


