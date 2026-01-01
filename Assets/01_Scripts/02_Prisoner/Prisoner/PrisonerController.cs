using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

// [통합됨] Actor의 기능을 모두 포함한 메인 컨트롤러
public class PrisonerController : MonoBehaviour
{
    // 1. 데이터 (기존 Actor의 변수들 대체)
    public PrisonerData Data { get; private set; }
    public CellAnchor AssignedCell { get; private set; }

    // 2. 컴포넌트 참조
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollSetting ragdoll;
    [SerializeField] private PrisonerSfxController sfx; 
    private PrisonerFSM fsm;
    private NavMeshAgent agent;


    // FSM에서 접근하기 쉽도록 프로퍼티 제공
    public bool IsSuspicious { get; private set; } // 수상함 여부
    public PrisonerAIType AIType => Data.RuntimeAIType;

    private void Awake()
    {
        // Awake에서는 컴포넌트 가져오기만 수행 (로직 실행 X)
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

    }



    // [Actor의 Init 대체] 스폰될 때 호출
    public void Initialize(PrisonerData data, CellAnchor cell, bool isSuspicious)
    {
        this.Data = data;
        this.AssignedCell = cell;
        this.IsSuspicious = isSuspicious;

        var fsm = GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            // 👇 [핵심] 앵커에 설정된 점검 위치를 FSM에 주입!
            if (cell.inspectionPoint != null)
            {
                fsm.InspectionPoint = cell.inspectionPoint;
                fsm.Setup(this, agent, animator);
            }
            else
            {
                Debug.LogError($"[Controller] {cell.name}에 inspectionPoint 할당되지 않았습니다!");
                // 임시 방편: 앵커 위치라도 쓰게 설정
                fsm.InspectionPoint = cell.transform;
            }
        }
        fsm.ChangeState(fsm.IdleState);

        Debug.Log($"<color=yellow>[Spawn]</color> 죄수 생성됨! " +
              $"ID: {data.definition.templateId} | " +
              $"이름: {data.definition.displayName} | " +
              $"특성(CSV): {data.definition.traitType} | " +
              $"오늘성향(Schedule): {data.RuntimeAIType} | " +
              $"수상함: {isSuspicious}");
    }

    // [Actor의 ApplyDamage 대체] 외부(총알 등)에서 호출하는 피격 함수
    public bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (Data.CurrentHealth <= 0) return false;

        // 1. 무적 상태 체크 (FSM에게 물어봄)
        if (fsm.IsInvulnerable) return false;

        // 2. 데이터 갱신
        Data.CurrentHealth -= dmg;

        // 3. 사망 처리
        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
            // 4. 생존 시 FSM에 알림 (반격 or 웅크리기)
            fsm.OnDamaged(dmg, hitPoint, hitDirection);
            if (sfx != null) sfx.PlayHitAndRandomMoan();
        }

        return true;
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        fsm.ChangeState(fsm.DeadState); // 상태 전환

        if (sfx != null) sfx.PlayRandomDieOnce();

        // 래그돌 처리
        if (ragdoll != null)
            ragdoll.ApplyImpact(hitPoint, hitDirection, 10f);

        // 이벤트 발생 등 추가 로직
        // PrisonerEventBus.RaisePrisonerDown(Data.ID);
    }
}