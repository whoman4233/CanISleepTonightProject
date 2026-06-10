using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PrisonerEscapeState : BasePrisonerState
{
    private Coroutine _sequenceCoroutine;
    private const float FLEE_DISTANCE = 25.0f; // 1차 목표 거리

    private bool _hasArrivedAtInspectionPoint = false;
    private bool _isEscaping = false;

    // 원래 속도를 기억할 변수
    private float _originalSpeed = 3.5f;

    // =======================================================================
    // Animator Hashes (성능 최적화용)
    // =======================================================================
    private static readonly int IsActionHash = Animator.StringToHash("IsAction");
    private static readonly int IsntStandingHash = Animator.StringToHash("IsntStanding");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int WalkHash = Animator.StringToHash("Walk");

    // RunStyle 파라미터 해시 추가
    private static readonly int RunStyleHash = Animator.StringToHash("RunStyle");

    // CrossFade용 상태(State) 이름 해시
    private static readonly int WalkStateHash = Animator.StringToHash("Prisoner_Walk01");
    private static readonly int IdleStateHash = Animator.StringToHash("Prisoner_Standing01");
    private static readonly int RunStateHash = Animator.StringToHash("Run");

    public PrisonerEscapeState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtInspectionPoint = false;
        _isEscaping = false;

        // 0. 원래 속도 가져오기 (데이터가 있으면 데이터 속도, 없으면 3.5)
        if (Controller.Data != null && Controller.Data.definition != null)
            _originalSpeed = Controller.Data.definition.spd > 0 ? Controller.Data.definition.spd : 3.5f;
        else
            _originalSpeed = 3.5f;

        Debug.Log($"[Escaper] {Controller.name}: 탈주 시퀀스 시작!");

        // 1. 플레이어 캐싱
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 2. 문 열기
        if (Controller.AssignedCell != null)
        {
            PrisonerEventBus.PublishForceOpenDoor(Controller.AssignedCell.cellId);
        }

        // ================================================================
        // 이전 행동 강제 초기화 (애니메이션 씹힘 방지)
        // ================================================================
        Controller.StopActionBehavior(); // 들고 있던 도구 제거, 소리 끄기
        Anim.SetBool(IsActionHash, false); // 특수 행동 파라미터 해제
        Anim.SetBool(IsntStandingHash, false); // 앉아/누워있었다면 기상
        Anim.SetBool(RunHash, false);

        // 3. 점호 위치로 이동 시작
        if (fsm.InspectionPoint != null)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = false;

                // 점호 위치로 갈 때는 걷기 속도로 강제 고정하여 미끄러짐 방지
                Agent.speed = 2.0f;
                Agent.acceleration = 12.0f;

                Agent.SetDestination(fsm.InspectionPoint.position);

                // 자연스러운 걷기 전이 유도 및 강제 전환
                Anim.SetBool(WalkHash, true);
                Anim.CrossFade(WalkStateHash, 0.1f);
            }
        }
        else
        {
            StartEscapeSequence();
        }
    }

    public override void Update()
    {
        if (Agent == null || !Agent.isOnNavMesh) return;

        // 1. 점호 위치로 걸어가는 단계
        if (!_hasArrivedAtInspectionPoint && !_isEscaping && fsm.InspectionPoint != null)
        {
            // 도착 체크
            if (!Agent.pathPending && Agent.remainingDistance <= 0.5f)
            {
                _hasArrivedAtInspectionPoint = true;
                StartEscapeSequence(); // 도착 -> 눈치보기 시작
            }
        }

        // 2. 도망치는 중일 때 멈추지 않고 계속해서 새로운 도주 경로 갱신
        if (_isEscaping)
        {
            // 목적지에 거의 다다르면 멈추지 않고 다음 도주 위치를 바로 갱신함
            if (!Agent.pathPending && Agent.remainingDistance <= 2.0f)
            {
                Vector3 fleePos = CalculateRobustFleePosition();
                Agent.SetDestination(fleePos);
            }
        }
    }

    private void StartEscapeSequence()
    {
        if (_sequenceCoroutine != null) fsm.StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = fsm.StartCoroutine(CoWaitAndRun());
    }

    private IEnumerator CoWaitAndRun()
    {
        // 1. 눈치 보기 (정지)
        if (Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        Anim.SetBool(WalkHash, false);
        Anim.SetBool(RunHash, false);
        Anim.CrossFade(IdleStateHash, 0.2f); // 자연스럽게 대기 모션

        // 2. 1.5초 대기
        yield return new WaitForSeconds(1.5f);

        // 3. 도주 시작
        RunAway();
    }

    private void RunAway()
    {
        _isEscaping = true;
        if (Agent.isOnNavMesh)
        {
            Agent.isStopped = false;

            // 도망칠 때는 달리기 속도 적용
            Agent.speed = 6.0f;

            Vector3 fleePos = CalculateRobustFleePosition();
            Agent.SetDestination(fleePos);
        }

        Anim.SetBool(WalkHash, false);

        // RunStyle 파라미터를 1로 설정하여 특정 달리기 스타일 지정
        Anim.SetInteger(RunStyleHash, 1);
        Anim.SetBool(RunHash, true);

        Anim.CrossFade(RunStateHash, 0.1f); // 바로 달리기 상태로 전환
    }

    private Vector3 CalculateRobustFleePosition()
    {
        Vector3 myPos = fsm.transform.position;
        Vector3 playerPos = (player != null) ? player.position : myPos - fsm.transform.forward;

        Vector3 fleeDir = (myPos - playerPos).normalized;
        if (fleeDir == Vector3.zero) fleeDir = fsm.transform.forward;

        // 1차: 25m, 2차: 15m, 3차: 5m 시도
        if (TryGetNavMeshPoint(myPos + fleeDir * FLEE_DISTANCE, out Vector3 result)) return result;
        if (TryGetNavMeshPoint(myPos + fleeDir * 15.0f, out result)) return result;
        if (TryGetNavMeshPoint(myPos + fleeDir * 5.0f, out result)) return result;

        return myPos + fleeDir * 2.0f;
    }

    private bool TryGetNavMeshPoint(Vector3 targetPos, out Vector3 result)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    public override void Exit()
    {
        if (_sequenceCoroutine != null)
        {
            fsm.StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        // 상태 나갈 때 정리
        Controller.StopActionBehavior();
        Anim.SetBool(RunHash, false);
        Anim.SetBool(WalkHash, false);
        Anim.SetBool(IsActionHash, false);

        // RunStyle 초기화 (다른 상태 진입 시 오류 방지)
        Anim.SetInteger(RunStyleHash, 0);

        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.speed = _originalSpeed; // 속도 원복
            Agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        fsm.ChangeState(fsm.CombatState);
    }
}