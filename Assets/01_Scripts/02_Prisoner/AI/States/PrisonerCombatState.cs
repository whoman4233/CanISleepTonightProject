using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _attackTimer = 0f;
    private const float AttackCooldown = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // 진입 시 0.5초 딜레이 (바로 때리면 부자연스러움)
        _attackTimer = 0.5f;

        agent.isStopped = false;
        anim.SetBool("Run", true);
    }

    public override void Update()
    {
        if (player == null) return;

        // 타이머 감소
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // ================================================================
        // [1] 공격 사거리 내부 (공격 시도)
        // ================================================================
        if (dist <= 1.5f)
        {
            // 확실하게 멈춤 처리
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // 플레이어 바라보기
            RotateTowardsPlayer();

            // 쿨타임 찼으면 공격
            if (_attackTimer <= 0f)
            {
                anim.SetTrigger("Attack");
                _attackTimer = AttackCooldown;
            }
        }
        // ================================================================
        // [2] 공격 사거리 밖 (추격)
        // ================================================================
        else
        {
            // ★ [수정] 경직/피격 상태(0.3초 이상 남음)라면 확실히 '정지' 시킴
            // 이 처리가 없으면 맞았는데도 미끄러지듯 이동하거나, 어정쩡하게 굳어버림
            if (_attackTimer >= 0.3f)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                anim.SetBool("Run", false);
            }
            // ★ [수정] 경직이 풀렸으면 다시 추격 재개
            else
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                anim.SetBool("Run", true);
            }
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");
        // 맞으면 경직 시간을 0.5초로 늘려서 "아파하는 모션" 동안 이동/공격 불가하게 함
        _attackTimer = Mathf.Max(_attackTimer, 0.5f);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }
    }
}