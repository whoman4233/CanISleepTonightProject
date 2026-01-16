using UnityEngine;

public sealed class PlayerAttackEventRelay : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Awake()
    {
        InitializePlayer();
    }

    private void InitializePlayer()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    public void AnimEvent_AttackHitboxOn()
    {
        // [수정] 혹시 씬 로드 후 player 연결이 끊겼다면 다시 찾기
        if (player == null) InitializePlayer();

        if (player != null && player.WeaponHandler != null)
        {
            player.WeaponHandler.SetHitColliderEnabled(true);
        }
    }

    public void AnimEvent_AttackHitboxOff()
    {
        if (player == null) InitializePlayer();

        if (player != null && player.WeaponHandler != null)
        {
            player.WeaponHandler.SetHitColliderEnabled(false);
        }
    }

    public void AnimEvent_AttackSwingSfx()
    {
        if (player == null) InitializePlayer();
        player?.Sfx?.PlayAttackSwingSfx();
    }
}