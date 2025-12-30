// [변경] PrisonerActor.cs -> PrisonerController.cs (메인 진입점)
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour, IDamageable
{
    // 데이터 컨테이너 보유
    public PrisonerData Data { get; private set; }

    // 컴포넌트 캐싱
    private PrisonerFSM fsm;
    private NavMeshAgent agent;
    private Animator animator;

    // 할당된 감옥 정보
    public CellAnchor AssignedCell { get; private set; }

    private void Awake()
    {
        fsm = GetComponent<PrisonerFSM>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    // 매니저가 호출하는 초기화 함수 (데이터 전달 통로)
    public void Initialize(PrisonerData data, CellAnchor cell)
    {
        this.Data = data;
        this.AssignedCell = cell;

        // FSM 시작
        fsm.Setup(this, agent, animator); // FSM에 컨트롤러(자신)을 넘겨서 데이터 접근 권한 부여
        fsm.ChangeState(PrisonerState.Idle);
    }

    public void TakeDamage(float amount)
    {
        Data.CurrentHealth -= amount;
        if (Data.CurrentHealth <= 0)
        {
            fsm.ChangeState(PrisonerState.Dead);
            // 글로벌 이벤트 버스로 사망 알림
            EventBus.Publish(new PrisonerDiedEvent(Data.ID));
        }
    }
}