using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _attackTimer = 0f;       // 쿨타임 체크용
    private const float AttackCooldown = 1.5f; // 공격 간격 (초)

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _attackTimer = 0f; // 시작하자마자 공격 가능하게 (혹은 딜레이 주려면 값 설정)
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
            agent.velocity = Vector3.zero; // 미끄러짐 방지
            anim.SetBool("Run", false);

            // 플레이어 쪽 바라보기 (회전)
            Vector3 dir = (player.position - fsm.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            }

            // ★ [수정] 쿨타임이 찼을 때만 공격 명령 1회 실행
            if (_attackTimer <= 0f)
            {
                anim.SetTrigger("Attack");
                _attackTimer = AttackCooldown; // 쿨타임 리셋
            }
        }
        else
        {
            // 추격
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("Run", true);
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");
        // 맞으면 공격 딜레이를 조금 늦추는 것도 자연스러움
        // _attackTimer = Mathf.Max(_attackTimer, 0.5f);
    }
}