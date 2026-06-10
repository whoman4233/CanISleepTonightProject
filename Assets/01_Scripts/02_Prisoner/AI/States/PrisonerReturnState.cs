using UnityEngine;

public class PrisonerReturnState : BasePrisonerState
{
    // [추가] 끼임 감지를 위한 타이머
    private float _stuckTimer = 0f;

    // ================================================================
    // Animator Hashes 캐싱
    // ================================================================
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
    private static readonly int HitCowerTriggerHash = Animator.StringToHash("HitCower");

    public PrisonerReturnState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter(); // BasePrisonerState의 Enter 호출

        // 1. 목표 지점 (침대/스폰 위치)
        Transform target = null;
        if (Controller.AssignedCell != null)
        {
            target = Controller.AssignedCell.prisonerSpawn;
        }

        if (target == null)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        // 특수 행동 자세 강제 초기화
        Controller.StopActionBehavior();

        _stuckTimer = 0f;

        float dist = Vector3.Distance(fsm.transform.position, target.position);
        if (dist > 0.5f)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);

                // [수정] Hash 사용 - 이동 애니메이션 시작
                anim.SetBool(WalkHash, true);
            }
            else
            {
                Debug.LogWarning($"[ReturnState] {Controller.name} is not on NavMesh. Force transition.");
                fsm.ChangeState(fsm.ActionState);
            }
        }
        else
        {
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Update()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        if (agent.velocity.sqrMagnitude < 0.1f)
        {
            _stuckTimer += Time.deltaTime;

            if (_stuckTimer > 2.0f)
            {
                fsm.ChangeState(fsm.ActionState);
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }

    public override void Exit()
    {
        // [수정] Hash 사용 - 나가면서 이동 애니메이션 끄기
        anim.SetBool(WalkHash, false);

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 1. 공격적인 성향 (반격)
        if (Controller.IsAggressive)
        {
            // [수정] Hash 사용
            anim.SetTrigger(HitTriggerHash);
            fsm.ChangeState(fsm.CombatState);
        }
        // 2. 소심한 성향 (겁먹음)
        else
        {
            // [수정] Hash 사용
            anim.SetTrigger(HitCowerTriggerHash);
            fsm.ChangeState(fsm.CowerState);
        }
    }
}