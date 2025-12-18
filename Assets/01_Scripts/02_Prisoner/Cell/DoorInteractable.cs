using UnityEngine;

public sealed class DoorInteractable : MonoBehaviour, IInteractable
{
    private static class AnimParams
    {
        public const string OpenTrigger = "Open";
        public const string CloseTrigger = "Close";
    }

    [Header("Refs")]
    [SerializeField] private CellDoorController doorController;
    [SerializeField] private Animator doorAnimator;

    // 현재 상태(토글용)
    private bool _isOpen;

    public void Interact(Player player)
    {
        Debug.Log("[DoorInteractable] Interact called");
        if (doorController == null || doorAnimator == null)
        {
            Debug.LogWarning("[DoorInteractable] Missing refs.", this);
            return;
        }

        // 게임 규칙 처리(입장/퇴장)
       // doorController.Interact(); // :contentReference[oaicite:1]{index=1}

        // 애니메이션 토글
        _isOpen = !_isOpen;
        if (_isOpen)
            doorAnimator.SetTrigger(AnimParams.OpenTrigger);
        else
            doorAnimator.SetTrigger(AnimParams.CloseTrigger);
    }
}