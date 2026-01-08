using UnityEngine;
using UnityEngine.AI;
// using static UnityEngine.ProBuilder.AutoUnwrapSettings; // ProBuilder API를 사용하지 않는다면 필요 없습니다.

public class PrisonerController : MonoBehaviour
{
    // 1. 데이터 (외부에서 읽기 전용)
    public PrisonerData Data { get; private set; }
    public CellAnchor AssignedCell { get; private set; }

    // 2. 컴포넌트 참조
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollSetting ragdoll;
    [SerializeField] private PrisonerSfxController sfx;
    private PrisonerFSM fsm;
    private NavMeshAgent agent;

    [Header("Visual Models (외형 모델)")]
    [SerializeField] private GameObject defaultModel;      // 기본 모델
    [SerializeField] private GameObject bikiniModel;       // 근육 비키니 모델 (3일차)
    [SerializeField] private GameObject goatHeadModel;     // 염소 머리 모델 (3일차)
    [SerializeField] private GameObject guardUniformModel; // 간수 복장 모델 (4일차 변장)

    // FSM에서 접근하기 쉽도록 프로퍼티 제공
    public bool IsSuspicious { get; private set; } // 수상한 죄수(범인)인가?
    public PrisonerAIType AIType => Data.RuntimeAIType; // 현재 AI 행동 타입

    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        // FSM이 없으면 자동으로 추가
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();
    }

    // [초기화 함수] 스폰될 때 호출됨
    public void Initialize(PrisonerData data, CellAnchor cell, bool isSuspicious)
    {
        this.Data = data;
        this.AssignedCell = cell;
        this.IsSuspicious = isSuspicious;

        // [비주얼 적용] 오늘의 역할(Role)에 따라 외형 변경
        // (PrisonerData 클래스에 dailyRole 필드가 있어야 합니다!)
        if (data.dailyRole.visualType != VisualAnomalyType.None)
        {
            ApplyVisualAnomaly(data.dailyRole.visualType);
        }
        else
        {
            // 설정이 없으면 기본값(None) 적용
            ApplyVisualAnomaly(VisualAnomalyType.None);
        }

        var fsm = GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            // 1. 점검 위치(InspectionPoint) 설정
            if (cell.inspectionPoint != null)
            {
                fsm.InspectionPoint = cell.inspectionPoint;
            }
            else
            {
                Debug.LogError($"[Controller] {cell.name}에 InspectionPoint가 없습니다! (임시로 Anchor 위치 사용)");
                fsm.InspectionPoint = cell.transform;
            }

            // 2. FSM 기본 셋업 (Controller, Agent, Animator 연결)
            fsm.Setup(this, agent, animator);

            // 3. 행동 초기화
            // 스케줄러가 지정해준 '오늘의 역할(AI Type)'로 초기 상태를 결정합니다.
            // (예: 1일차 소음 유발자면 SingingState 등으로 시작)
            fsm.InitializeBehavior(data.RuntimeAIType);
        }
    }

    // [피격 함수] 외부(플레이어 무기 등)에서 호출
    public bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        // 이미 죽었으면 무시
        if (Data.CurrentHealth <= 0) return false;

        // 1. 무적 상태 체크 (FSM에게 위임)
        if (fsm.IsInvulnerable) return false;

        // 2. 체력 감소
        Data.CurrentHealth -= dmg;

        // 3. 사망 판정
        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
            // 4. 생존 시: FSM에 알림 (반격하거나 웅크리기)
            fsm.OnDamaged(dmg, hitPoint, hitDirection);

            // 피격음 및 신음 소리 재생
            if (sfx != null) sfx.PlayHitAndRandomMoan();
        }

        return true;
    }

    // 사망 처리
    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        fsm.ChangeState(fsm.DeadState); // 상태 전환 (죽음)

        if (sfx != null) sfx.PlayRandomDieOnce(); // 사망 비명

        // 래그돌 물리 효과 적용
        if (ragdoll != null)
            ragdoll.ApplyImpact(hitPoint, hitDirection, 10f);

        // 이벤트 발생 (게임 로직에 알림)
        PrisonerEventBus.RaisePrisonerDown(Data.ID);
    }

    // [외형 변경 로직]
    private void ApplyVisualAnomaly(VisualAnomalyType visualType)
    {
        // 1. 일단 모든 모델을 끄고 기본 모델만 켬 (초기화)
        if (defaultModel) defaultModel.SetActive(true);
        if (bikiniModel) bikiniModel.SetActive(false);
        if (goatHeadModel) goatHeadModel.SetActive(false);
        if (guardUniformModel) guardUniformModel.SetActive(false);

        // 2. 타입에 따라 특정 모델 활성화
        switch (visualType)
        {
            case VisualAnomalyType.BikiniModel: // Enum 이름 확인 필요 (BikiniModel인지 MuscleBikini인지)
                if (defaultModel) defaultModel.SetActive(false);
                if (bikiniModel) bikiniModel.SetActive(true);
                break;

            case VisualAnomalyType.GoatHead:
                // 염소 머리는 기본 몸 위에 머리만 씌우는 방식이라면 defaultModel을 끄지 않을 수도 있음
                // 여기서는 머리만 교체한다고 가정
                if (goatHeadModel) goatHeadModel.SetActive(true);
                break;

            case VisualAnomalyType.Imposter_Guard:
                if (defaultModel) defaultModel.SetActive(false);
                if (guardUniformModel) guardUniformModel.SetActive(true);
                break;

            case VisualAnomalyType.None:
            default:
                // 위에서 이미 기본값으로 초기화했으므로 추가 동작 없음
                break;
        }
    }

    // PrisonerController.cs 내부에 추가

    // [매핑 테이블] AIType -> Animator BlendTree 번호
    private int GetActionAnimID(PrisonerAIType type)
    {
        return type switch
        {
            // 0번: 아무것도 안 함 (기본 Idle)
            PrisonerAIType.Good => 0,
            PrisonerAIType.Bad => 0,

            // 1일차 소음/특수 행동
            PrisonerAIType.Singing => 1,
            PrisonerAIType.Screaming => 2,
            PrisonerAIType.Mumbling => 3,
            PrisonerAIType.HammeringWall => 4,
            PrisonerAIType.Deadlift => 5,
            PrisonerAIType.Crying => 6,

            // 3일차/7일차 행동
            PrisonerAIType.Escaper => 7,   // 땅파기 (Digging)
            PrisonerAIType.Graffiti => 8,  // 낙서
            PrisonerAIType.Ambusher => 9,  // 기습 대기 (숨기)

            _ => 0 // 그 외는 기본 대기
        };
    }

    public void StartActionBehavior(PrisonerAIType type)
    {
        // 1. 애니메이션 전환 (Blend Tree의 ActionType 파라미터 변경)
        int animID = GetActionAnimID(type);
        if (animator != null) animator.SetInteger("ActionType", animID);

        // 2. ★ 소리 재생 (Switch문 삭제됨! 훨씬 깔끔)
        if (sfx != null)
        {
            sfx.PlayLoop(type);
        }

        // 3. 도구(Prop) 들기
        // 예: 망치질이면 망치 오브젝트 켜기
        // if (type == PrisonerAIType.HammeringWall && hammerObj != null) hammerObj.SetActive(true);
    }

    public void StopActionBehavior()
    {
        // 1. 애니메이션 복구 (Normal Idle)
        if (animator != null) animator.SetInteger("ActionType", 0);

        // 2. 소리 끄기
        if (sfx != null) sfx.StopAllLoops();

        // 3. 도구 숨기기
        // if (hammerObj != null) hammerObj.SetActive(false);
    }
}