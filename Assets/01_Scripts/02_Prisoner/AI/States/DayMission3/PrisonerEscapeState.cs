using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PrisonerEscapeState : BasePrisonerState
{
    private Coroutine _escapeCoroutine;
    private const float FLEE_DISTANCE = 20.0f; // 도망칠 목표 거리

    public PrisonerEscapeState(PrisonerFSM fsm) : base(fsm)
    {
        // 생성자에서 특별히 할 것은 없음
    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log($"[AI] {Controller.name}: 문이 열렸다! 3초 뒤 탈주한다...");

        // 0. 플레이어 찾기 (도망칠 기준점)
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 1. 문 열기 (상태 진입과 동시에 문 개방)
        if (Controller.AssignedCell != null)
        {
            PrisonerEventBus.PublishForceOpenDoor(Controller.AssignedCell.cellId);
        }

        // 2. 일단 대기 (3초간 멍때리기 or 눈치보기)
        Anim.SetBool("Walk", false);
        Anim.SetBool("Run", false);

        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true; // 이동 정지
            Agent.velocity = Vector3.zero;
        }

        // 3. 3초 후 도망 로직 시작
        if (_escapeCoroutine != null) fsm.StopCoroutine(_escapeCoroutine);
        _escapeCoroutine = fsm.StartCoroutine(CoStartEscape());
    }

    private IEnumerator CoStartEscape()
    {
        // 3초 대기
        yield return new WaitForSeconds(3.0f);

        Debug.Log($"[AI] {Controller.name}: 으아아! 도망쳐! (플레이어 반대 방향)");

        // 4. 애니메이션 Run 켜기
        Anim.SetBool("Run", true);

        if (Agent != null && Agent.isOnNavMesh && player != null)
        {
            Agent.isStopped = false;
            Agent.speed = 6.0f; // 평소보다 빠르게 설정 (기본 3.5~4.0 가정)

            // ★ [핵심] 플레이어 반대 방향 벡터 계산
            Vector3 fleeDirection = (fsm.transform.position - player.position).normalized;

            // 혹시 겹쳐서 방향이 0이면 랜덤 방향
            if (fleeDirection == Vector3.zero) fleeDirection = Random.insideUnitSphere.normalized;
            fleeDirection.y = 0; // 높이 무시

            // 현재 위치에서 반대 방향으로 20m 떨어진 지점 계산
            Vector3 targetPos = fsm.transform.position + fleeDirection * FLEE_DISTANCE;

            // NavMesh 위에서 갈 수 있는 유효한 좌표 찾기 (반경 5m 내 탐색)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 10.0f, NavMesh.AllAreas))
            {
                Agent.SetDestination(hit.position);
            }
            else
            {
                // 못 찾으면 그냥 해당 방향으로 직진 명령
                Agent.SetDestination(targetPos);
            }
        }

        _escapeCoroutine = null;
    }

    public override void Update()
    {
        // 도망 중일 때 도착 체크
        if (Agent != null && Agent.isOnNavMesh && !Agent.isStopped)
        {
            // 경로 계산 중이 아니고, 남은 거리가 1m 미만이면
            if (!Agent.pathPending && Agent.remainingDistance < 1.0f)
            {
                Debug.Log($"[AI] {Controller.name}: 도주 성공 (사라짐)");
                Controller.gameObject.SetActive(false); // 게임 오브젝트 비활성화 (사라짐)
            }
        }
    }

    public override void Exit()
    {
        // 상태 나갈 때 코루틴 정리
        if (_escapeCoroutine != null)
        {
            fsm.StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }

        Anim.SetBool("Run", false);

        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.ResetPath();
            Agent.speed = 2.0f; // 속도 원복 (기본 걷기 속도)
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 도망가다가 맞으면 전투 태세로 전환
        fsm.ChangeState(fsm.CombatState);
    }
}