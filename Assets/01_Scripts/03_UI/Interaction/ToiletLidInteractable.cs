using UnityEngine;

public class ToiletLidInteractable : MonoBehaviour, IInteractable
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    private int _openTriggerHash;
    private int _closeTriggerHash;

    private bool _isOpen;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _openTriggerHash = Animator.StringToHash(openTriggerName);
        _closeTriggerHash = Animator.StringToHash(closeTriggerName);
    }

    public void Interact(Player player)
    {
        ToggleLid();
    }

    private void ToggleLid()
    {
        if (_isOpen)
        {
            animator.SetTrigger(_closeTriggerHash);
        }
        else
        {
            animator.SetTrigger(_openTriggerHash);
        }

        _isOpen = !_isOpen;
    }
}
