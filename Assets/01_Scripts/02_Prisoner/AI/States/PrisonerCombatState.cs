using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _attackTimer = 0f;
    private const float AttackCooldown = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // [수정] 진입 시 즉시 공격하지 않고, 피격 모션을 보여줄 시간(0.5초~1초)을 벎
        // 이렇게 하면 "맞자마자 반격"하는 부자연스러운 동작도 사라지고, 애니메이션 씹힘도 방지됨
        _attackTimer = 0.5f; // 0.5초 딜레이

        // 추격 시작
        agent.isStopped = false;
        anim.SetBool("Run", true);
    }

    public override void Update()
    {
        if (player == null) return;

        // 타이머 감소
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= 1.5f) // 공격 사거리
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            Vector3 dir = (player.position - fsm.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            }

            // 쿨타임 찼으면 공격
            if (_attackTimer <= 0f)
            {
                anim.SetTrigger("Attack");
                _attackTimer = AttackCooldown;
            }
        }
        else
        {
            // [추가] 공격 타이머가(경직이) 조금 남았어도 거리가 멀면 이동은 가능하게 처리
            // 단, 너무 짧은 찰나의 이동 방지를 위해 0.2초 정도는 기다림
            if (_attackTimer < 0.3f)
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
        // [추가] 전투 중 맞으면 공격 쿨타임을 늘려서 "아파하는" 시간 확보 (연타 방지)
        _attackTimer = Mathf.Max(_attackTimer, 0.5f);
    }
}