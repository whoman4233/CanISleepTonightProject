using UnityEngine;

public sealed class PlayerAttackEventRelay : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    // 애니메이션 이벤트에서 호출
    public void AnimEvent_AttackHitboxOn()
    {
        player?.WeaponHandler?.SetHitColliderEnabled(true);
    }

    public void AnimEvent_AttackHitboxOff()
    {
        player?.WeaponHandler?.SetHitColliderEnabled(false);
    }
    public void AnimEvent_AttackSwingSfx()
    {
        player?.Sfx?.PlayAttackSwingSfx();
    }
}