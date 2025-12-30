using UnityEngine;

[RequireComponent(typeof(PrisonerActor))]
public class PrisonerBadAI : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.2f;

    private PrisonerActor _actor;
    private Transform _player;
    private float _cooldown;

    private void Awake()
    {
        _actor = GetComponent<PrisonerActor>();

        // ✅ 스폰 직후(Init 전에) 1프레임이라도 도는 것 방지
        enabled = false;
    }

    public void BindPlayer(Transform player)
    {
        _player = player;
    }

    private void Update()
    {
        if (_player == null) return;

        // ✅ 2중 잠금: 진압(전투) 전에는 절대 실행 불가
        if (!_actor.CombatEnabled) return;

        if (!_actor.IsAlive) return;
        if (_actor.Type != PrisonerAIType.Bad) return;

        // 이동(직선)
        var dir = (_player.position - transform.position);
        dir.y = 0f;
        var dist = dir.magnitude;

        if (dist > attackRange)
        {
            var move = dir.normalized * Mathf.Max(0, _actor.Spd) * Time.deltaTime;
            transform.position += move;
            return;
        }

        // 공격
        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        _cooldown = attackCooldown;

        // TODO: 플레이어 체력에 데미지 적용
    }
}
