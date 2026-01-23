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
        public GameObject propObject;
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

        InitializeDictionaries();

        // ★ [추가] 시작 시 도구들을 자동으로 손 뼈 하위로 이동시킴
        AutoAttachPropsToHand();
    }

    private void InitializeDictionaries()
    {
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

    // ★ [추가] 프롭 자동 장착 로직
    private void AutoAttachPropsToHand()
    {
        if (animator == null) return;

        // 1. 애니메이터에서 오른손 뼈를 찾음 (Humanoid Rig 필수)
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightHand == null)
        {
            // Humanoid가 아니거나 뼈 세팅이 안된 경우
            // Debug.LogWarning($"[PrisonerController] {name}: RightHand 뼈를 찾을 수 없습니다. (Generic Rig?)");
            return;
        }

        // 2. 등록된 모든 프롭을 손 뼈 자식으로 이동
        foreach (var data in actionProps)
        {
            if (data.propObject != null)
            {
                // 부모를 손으로 변경
                data.propObject.transform.SetParent(rightHand);

                // ★ 위치와 회전을 0으로 초기화하여 손에 '착' 달라붙게 함
                // (모델의 Pivot이 손잡이 위치여야 자연스럽습니다)
                data.propObject.transform.localPosition = Vector3.zero;
                data.propObject.transform.localRotation = Quaternion.identity;

                // 필요하다면 스케일 초기화 (상황에 따라 주석 처리 가능)
                // data.propObject.transform.localScale = Vector3.one;
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

        if (animator != null)
        {
            animator.SetBool("Suspicious", IsSuspicious);
        }

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
        Collider[] hits = new Collider[20];
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, hits, targetLayer);

        if (count == 0)
        {
            // Debug.Log($"[Combat] {name} 공격 휘두름 - 허공 (TargetLayer 감지 실패)");
        }

        for (int i = 0; i < count; i++)
        {
            var target = hits[i];
            if (target.gameObject == gameObject) continue;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            dirToTarget.y = 0;
            Vector3 myForward = transform.forward;
            myForward.y = 0;

            if (Vector3.Angle(myForward, dirToTarget) < attackAngle)
            {
                var playerHealth = target.GetComponent<Health>();

                if (playerHealth != null)
                {
                    int finalDamage = (Data != null && Data.AttackPower > 0) ? (int)Data.AttackPower : 10;
                    playerHealth.TakeDamage(finalDamage);
                    Debug.Log($"[Combat] {name}가 {target.name} 타격! (DMG: {finalDamage})");
                }
                else
                {
                    Debug.LogWarning($"[Combat] {target.name} 감지했으나 Health 컴포넌트 없음!");
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