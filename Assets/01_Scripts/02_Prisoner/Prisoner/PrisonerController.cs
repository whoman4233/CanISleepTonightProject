using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    private const float RagdollImpactForce = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAngle = 45f; // 전방 부채꼴 범위
    [SerializeField] private LayerMask targetLayer;   // Player 레이어 설정

    // ================================================================
    // [1] 데이터 정의
    // ================================================================

    [System.Serializable]
    public struct ActionPropData
    {
        public PrisonerAIType type;    // 예: HammeringWall
        public GameObject propObject;  // 예: 손에 쥐어준 망치 (이 프리팹의 자식)
    }

    // ================================================================
    // [2] 컴포넌트 및 변수
    // ================================================================

    public PrisonerData Data { get; private set; }
    public CellAnchor AssignedCell { get; private set; }

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollSetting ragdoll;
    [SerializeField] private PrisonerSfxController sfx;
    private PrisonerFSM fsm;
    private NavMeshAgent agent;

    [Header("Action Props (Tools)")]
    // 이 프리팹이 사용할 도구들 (각 프리팹마다 손 위치에 맞게 세팅 필요)
    [SerializeField] private List<ActionPropData> actionProps;

    private Dictionary<PrisonerAIType, GameObject> _propMap;

    public bool IsSuspicious { get; private set; }
    public PrisonerAIType AIType => Data != null ? Data.RuntimeAIType : PrisonerAIType.Good;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        // 도구 맵핑
        _propMap = new Dictionary<PrisonerAIType, GameObject>();
        if (actionProps != null)
        {
            foreach (var data in actionProps)
            {
                if (data.propObject != null && !_propMap.ContainsKey(data.type))
                    _propMap.Add(data.type, data.propObject);
            }
        }
    }

    // [수정] 스폰 컨트롤러와 호환되도록 원래 파라미터(cell, isSuspicious) 유지
    public void Initialize(PrisonerData data, CellAnchor cell, bool isSuspicious)
    {
        this.Data = data;

        // ================================================================
        // [핵심] 스탯 동기화 및 안전장치 (데이터 -> 실제 적용)
        // ================================================================
        if (this.Data != null)
        {
            // 1. 공격력 초기화 (데이터에 없거나 0이면 기본값 10 부여)
            if (this.Data.AttackPower <= 0) this.Data.AttackPower = 10f;

            // 2. 체력 초기화 (데이터에 없거나 0이면 기본값 100 부여)
            if (this.Data.MaxHealth <= 0) this.Data.MaxHealth = 100f;

            // 3. 현재 체력을 최대 체력으로 리셋 (재사용 시 필수)
            this.Data.CurrentHealth = this.Data.MaxHealth;
        }

        this.AssignedCell = cell;
        this.IsSuspicious = isSuspicious;

        // NavMeshAgent 설정 (멈춤 현상 방지용)
        if (agent != null && data != null && data.definition != null)
        {
            agent.speed = data.definition.spd > 0 ? data.definition.spd : 3.5f;
            agent.enabled = true;
        }

        // FSM 초기화
        if (fsm != null)
        {
            fsm.Setup(this, agent, animator);
            fsm.InitializeBehavior(data.RuntimeAIType);
        }

        Debug.Log($"[Prisoner Spawn] ID:{(Data != null ? Data.Name : "null")} | HP:{Data?.CurrentHealth} | ATK:{Data?.AttackPower}");
    }

    // ================================================================
    // [3] 행동 (Action) 관련
    // ================================================================

    // 행동 시작
    public void StartActionBehavior(PrisonerAIType type)
    {
        if (animator != null) animator.SetFloat("ActionType", GetActionAnimID(type));
        if (sfx != null) sfx.PlayLoop(type);

        if (_propMap.TryGetValue(type, out GameObject prop))
        {
            if (prop != null) prop.SetActive(true);
        }
    }

    // 행동 종료
    public void StopActionBehavior()
    {
        if (animator != null) animator.SetFloat("ActionType", 0);
        if (sfx != null) sfx.StopLoop();

        foreach (var prop in _propMap.Values)
        {
            if (prop != null) prop.SetActive(false);
        }
    }

    private int GetActionAnimID(PrisonerAIType type)
    {
        return type switch
        {
            PrisonerAIType.Singing => 1,
            PrisonerAIType.Screaming => 2,
            PrisonerAIType.Mumbling => 3,
            PrisonerAIType.HammeringWall => 4,
            PrisonerAIType.Deadlift => 5,
            PrisonerAIType.Crying => 6,
            PrisonerAIType.Escaper => 7,
            PrisonerAIType.Graffiti => 8,
            PrisonerAIType.Ambusher => 9,
            _ => 0
        };
    }

    // ================================================================
    // [4] 피격 및 사망 처리
    // ================================================================

    public virtual bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (Data == null || Data.CurrentHealth <= 0) return false;

        // 1. 체력 데이터 깎기
        Data.CurrentHealth -= dmg;

        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection); // 사망 처리
        }
        else
        {
            // 2. FSM에게 피격 알림
            fsm.OnDamaged(dmg, hitPoint, hitDirection);

            // 3. 사운드 재생
            if (sfx != null) sfx.PlayHitAndRandomMoan();
        }
        return true;
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        StopActionBehavior();

        if (sfx != null)
        {
            sfx.StopAllSounds();
            sfx.PlayRandomDieOnce();
        }

        fsm.ChangeState(fsm.DeadState);

        if (ragdoll != null) ragdoll.ApplyImpact(hitPoint, hitDirection, RagdollImpactForce);

        PrisonerEventBus.RaisePrisonerDown(Data.ID);
    }

    // ================================================================
    // ★ [핵심 수정] 애니메이션 이벤트에서 호출되는 공격 함수
    // ================================================================
    public void OnAttackHitCheck()
    {
        // 버퍼 크기 증가 (안정성 확보)
        Collider[] hits = new Collider[20];

        // 내 위치에서 공격 사거리만큼 검사
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, hits, targetLayer);

        for (int i = 0; i < count; i++)
        {
            var target = hits[i];

            if (target.gameObject == gameObject) continue;

            // 부채꼴 각도 계산
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            dirToTarget.y = 0;
            Vector3 myForward = transform.forward;
            myForward.y = 0;

            if (Vector3.Angle(myForward, dirToTarget) < attackAngle)
            {
                var playerHealth = target.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // ★ [수정] 하드코딩(10) 제거 -> 데이터 공격력 사용
                    // 데이터가 없거나 0이면 최소 1데미지라도 주도록 설정
                    int finalDamage = (Data != null && Data.AttackPower > 0) ? (int)Data.AttackPower : 10;

                    playerHealth.TakeDamage(finalDamage);
                    Debug.Log($"[Prisoner] {Data?.Name ?? "Unknown"}가 플레이어를 공격! (피해량: {finalDamage})");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 기습 범위 그리기
        if (AIType == PrisonerAIType.Ambusher)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 3.5f);
        }

        // 공격 범위 디버깅용
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}