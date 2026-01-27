using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    private const float RagdollImpactForce = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAngle = 90f; // 공격 각도 완화 (45 -> 90)
    [SerializeField] private LayerMask targetLayer;

    // ================================================================
    // [1] 데이터 정의
    // ================================================================

    [System.Serializable]
    public struct ActionPropData
    {
        public PrisonerAIType type;
        public GameObject propObject; // 프리팹 원본
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

    // 실제 생성된 오브젝트들을 관리하는 딕셔너리
    private Dictionary<PrisonerAIType, GameObject> _propMap;

    public bool IsSuspicious { get; private set; }
    public PrisonerAIType AIType => Data != null ? Data.RuntimeAIType : PrisonerAIType.Good;

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

        _propMap = new Dictionary<PrisonerAIType, GameObject>();

        // 프롭 생성 및 손에 부착 (초기엔 다 꺼둠)
        AutoAttachPropsToHand();
    }

    private void AutoAttachPropsToHand()
    {
        if (animator == null) return;

        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null) return;

        foreach (var data in actionProps)
        {
            if (data.propObject != null)
            {
                GameObject propInstance = Instantiate(data.propObject);
                propInstance.name = data.propObject.name;
                propInstance.transform.SetParent(rightHand);
                propInstance.transform.localPosition = Vector3.zero;
                propInstance.transform.localRotation = Quaternion.identity;

                propInstance.SetActive(false);

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

        // 내 역할(AIType)에 맞는 무기가 있다면 생성 즉시 활성화
        // (InitializeBehavior가 호출되기 전부터 들고 있게 함)
        PrisonerAIType myType = data.RuntimeAIType;
        if (_propMap.TryGetValue(myType, out GameObject myWeapon))
        {
            if (myWeapon != null)
            {
                myWeapon.SetActive(true);
            }
        }

        if (fsm != null)
        {
            fsm.Setup(this, agent, animator);
            fsm.InitializeBehavior(data.RuntimeAIType);
        }

        Debug.Log($"[Prisoner Spawn] ID:{(Data != null ? Data.Name : "null")} | Type:{AIType} | HasWeapon:{HasWeapon}");
    }

    // Enum 기반 행동 시작
    public void StartActionBehavior(PrisonerAIType type)
    {
        if (animator != null) animator.SetBool("IsAction", true);
        if (animator != null) animator.SetFloat("ActionType", GetActionAnimID(type));
        if (sfx != null) sfx.PlayLoop(type);

        // 요청된 무기는 켜고, 나머지는 끈다. (중복 장착 방지)
        // StopActionBehavior에서 끄는 로직을 없앴으므로 여기서 정리해줘야 함.
        foreach (var kvp in _propMap)
        {
            PrisonerAIType key = kvp.Key;
            GameObject prop = kvp.Value;

            if (prop == null) continue;

            if (key == type)
            {
                prop.SetActive(true);
            }
            else
            {
                prop.SetActive(false);
            }
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

        // 무기를 끄는 코드 삭제!
        // 이제 행동이 멈춰도(이동 중 등) 무기는 손에 계속 들려있습니다.
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

        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var target = hits[i];
            if (target.gameObject == gameObject) continue;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            dirToTarget.y = 0;
            Vector3 myForward = transform.forward;
            myForward.y = 0;

            // 각도 체크 (90도)
            if (Vector3.Angle(myForward, dirToTarget) < 90f)
            {
                var playerHealth = target.GetComponent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInParent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInChildren<Health>();

                if (playerHealth != null)
                {
                    int finalDamage = (Data != null && Data.AttackPower > 0) ? (int)Data.AttackPower : 10;
                    playerHealth.TakeDamage(finalDamage);
                    Debug.Log($"✅ [Hit Success] {name} -> Player ({finalDamage} dmg)");
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