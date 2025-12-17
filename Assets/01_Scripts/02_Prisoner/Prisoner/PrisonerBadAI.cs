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
    }

    public void BindPlayer(Transform player)
    {
        _player = player;
    }

    private void Update()
    {
        if (_player == null) return;
        if (!_actor.IsAlive) return;
        if (_actor.type != PrisonerType.Bad) return;

        // 이동(직선)
        var dir = (_player.position - transform.position);
        dir.y = 0f;
        var dist = dir.magnitude;

        if (dist > attackRange)
        {
            var move = dir.normalized * Mathf.Max(0, _actor.spd) * Time.deltaTime;
            transform.position += move;
            return;
        }

        // 공격
        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        _cooldown = attackCooldown;

        //// 플레이어 체력에 데미지
    }
}
