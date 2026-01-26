using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    private const float RagdollImpactForce = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAngle = 45f;
    [SerializeField] private LayerMask targetLayer;

    // ================================================================
    // [1] 데이터 정의
    // ================================================================

    [System.Serializable]
    public struct ActionPropData
    {
        public PrisonerAIType type;
        public GameObject propObject; // 여기엔 프리팹 원본이 들어갑니다
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
    [SerializeField] private List<ActionPropData> actionProps;

    // 실제 게임에서 제어할 '생성된' 오브젝트들을 담는 곳
    private Dictionary<PrisonerAIType, GameObject> _propMap;

    public bool IsSuspicious { get; private set; }
    public PrisonerAIType AIType => Data != null ? Data.RuntimeAIType : PrisonerAIType.Good;

    // ================================================================
    // 성향 및 전투 판별 프로퍼티
    // ================================================================

    public bool IsAggressive => CheckAggressiveType(AIType);
    public bool HasWeapon => IsWeaponUser(AIType);

    private bool CheckAggressiveType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Bad ||
               type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Escaper ||
               type == PrisonerAIType.Attacking;
    }

    private bool IsWeaponUser(PrisonerAIType type)
    {
        switch (type)
        {
            case PrisonerAIType.HammeringWall:
            case PrisonerAIType.Ambusher:
                return true;
            default:
                return false;
        }
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

        // 딕셔너리 초기화 (내용물은 아래 AutoAttach 함수에서 채웁니다)
        _propMap = new Dictionary<PrisonerAIType, GameObject>();

        // ★ [수정] 프롭을 '생성'하고 손에 붙인 뒤 딕셔너리에 등록
        AutoAttachPropsToHand();
    }

    // ★ [핵심 수정] 프리팹 인스턴스화 및 자동 장착 로직
    private void AutoAttachPropsToHand()
    {
        if (animator == null) return;

        // 1. 애니메이터에서 오른손 뼈를 찾음 (Humanoid Rig 필수)
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightHand == null)
        {
            // Debug.LogWarning($"[PrisonerController] {name}: RightHand 뼈를 찾을 수 없습니다.");
            return;
        }

        // 2. 등록된 프롭 프리팹들을 하나씩 순회
        foreach (var data in actionProps)
        {
            if (data.propObject != null)
            {
                // [중요] 프리팹 원본을 바로 자식으로 넣으면 에러가 납니다.
                // 반드시 Instantiate(복제/생성)를 해야 씬(Scene)에 존재하는 오브젝트가 됩니다.

                GameObject propInstance = Instantiate(data.propObject);

                // (Clone) 이름 제거 (선택사항)
                propInstance.name = data.propObject.name;

                // 부모를 손으로 설정
                propInstance.transform.SetParent(rightHand);

                // 위치와 회전을 0으로 초기화하여 손에 '착' 달라붙게 함
                propInstance.transform.localPosition = Vector3.zero;
                propInstance.transform.localRotation = Quaternion.identity;
                // 필요 시 스케일 초기화: propInstance.transform.localScale = Vector3.one;

                // 평소엔 안 보이게 끔
                propInstance.SetActive(false);

                // ★ [중요] '복제된 실체'를 딕셔너리에 등록해야 나중에 켜고 끄기가 가능
                if (!_propMap.ContainsKey(data.type))
                {
                    _propMap.Add(data.type, propInstance);
                }
            }
        }
    }

    public void Initialize(PrisonerData data, CellAnchor cell, bool isSuspicious)
    {
        this.Data = data;

        if (this.Data != null)
        {
            if (this.Data.AttackPower <= 0) this.Data.AttackPower = 10f;
            if (this.Data.MaxHealth <= 0) this.Data.MaxHealth = 100f;
            this.Data.CurrentHealth = this.Data.MaxHealth;
        }

        this.AssignedCell = cell;
        this.IsSuspicious = isSuspicious;

        if (agent != null && data != null && data.definition != null)
        {
            agent.speed = data.definition.spd > 0 ? data.definition.spd : 3.5f;
            agent.enabled = true;
        }

        if (fsm != null)
        {
            fsm.Setup(this, agent, animator);
            fsm.InitializeBehavior(data.RuntimeAIType);
        }

        Debug.Log($"[Prisoner Spawn] ID:{(Data != null ? Data.Name : "null")} | Type:{AIType} | HasWeapon:{HasWeapon} | Aggressive:{IsAggressive}");
    }

    // [기존] Enum 기반 행동 시작
    public void StartActionBehavior(PrisonerAIType type)
    {
        if (animator != null) animator.SetBool("IsAction", true);
        if (animator != null) animator.SetFloat("ActionType", GetActionAnimID(type));
        if (sfx != null) sfx.PlayLoop(type);

        if (_propMap.TryGetValue(type, out GameObject prop))
        {
            if (prop != null) prop.SetActive(true);
        }
    }

    public void StartActionBehavior(int rawAnimID)
    {
        if (animator != null)
        {
            animator.SetFloat("ActionType", (float)rawAnimID);
        }
    }

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
            PrisonerAIType.Digging => 10,
            PrisonerAIType.Attacking => 11,
            PrisonerAIType.Suss => 12,
            _ => 0
        };
    }

    public virtual bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (Data == null || Data.CurrentHealth <= 0) return false;

        Data.CurrentHealth -= dmg;

        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
            fsm.OnDamaged(dmg, hitPoint, hitDirection);
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

    public void OnAttackHitCheck()
    {
        // 1. 공격 범위 디버깅 (Scene 뷰에서 확인용)
        // Debug.DrawRay(transform.position + Vector3.up, transform.forward, Color.red, 1.0f);

        Collider[] hits = new Collider[20];
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, hits, targetLayer);

        if (count == 0) return; // 감지된 게 없으면 종료

        for (int i = 0; i < count; i++)
        {
            var target = hits[i];
            if (target.gameObject == gameObject) continue;

            // 방향 벡터 계산
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            dirToTarget.y = 0;
            Vector3 myForward = transform.forward;
            myForward.y = 0;

            // ★ [수정] 각도 완화 (45도 -> 90도)
            // 너무 정면만 때려야 하면 빗나가는 느낌이 듭니다. 반경 180도(좌우 90)까지 허용 추천
            if (Vector3.Angle(myForward, dirToTarget) < 90f) // attackAngle 대신 90f 하드코딩 혹은 Inspector값 증가
            {
                // ★ [수정] Health 컴포넌트 탐색 강화 (부모/자식 모두 검색)
                var playerHealth = target.GetComponent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInParent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInChildren<Health>();

                if (playerHealth != null)
                {
                    int finalDamage = (Data != null && Data.AttackPower > 0) ? (int)Data.AttackPower : 10;

                    playerHealth.TakeDamage(finalDamage);

                    Debug.Log($"✅ [Hit Success] {name} -> Player ({finalDamage} dmg)");

                    // 한 번에 한 명만 때리려면 여기서 return; (광역 공격이면 유지)
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AIType == PrisonerAIType.Ambusher)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 3.5f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}