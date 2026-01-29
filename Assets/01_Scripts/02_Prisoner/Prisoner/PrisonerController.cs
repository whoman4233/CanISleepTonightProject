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

    // ★ [추가] 태어난 위치(복귀 지점)를 기억할 변수
    public Vector3 SpawnPosition { get; private set; }

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
        // ★ [추가] 현재 위치를 스폰 위치로 저장
        SpawnPosition = transform.position;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

        _propMap = new Dictionary<PrisonerAIType, GameObject>();

        // 프롭 생성 및 손에 부착 (초기엔 다 꺼둠)
        AutoAttachPropsToHand();
    }

    // ★ [추가] 안전장치: Awake 시점에 위치가 0,0,0이었다면 Start에서 다시 갱신
    private void Start()
    {
        if (SpawnPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            SpawnPosition = transform.position;
        }
    }

    private void AutoAttachPropsToHand()
    {
        if (animator == null) return;

        // 1. 오른손 본 찾기
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null) return;

        // ★ [수정] "Weapon_R" 찾기 로직 추가
        // 기본값은 오른손(rightHand)으로 설정하되, Weapon_R을 발견하면 교체합니다.
        Transform targetParent = rightHand;

        // 직계 자식 중에서 "Weapon_R" 이름을 가진 오브젝트 탐색
        Transform weaponMount = rightHand.Find("Weapon_R");

        // (선택 사항: 만약 계층구조가 깊어서 Find로 안 찾아진다면 아래 주석을 풀어 깊은 탐색을 사용하세요)
        /*
        if (weaponMount == null) {
            foreach (Transform t in rightHand.GetComponentsInChildren<Transform>()) {
                if (t.name == "Weapon_R") { weaponMount = t; break; }
            }
        }
        */

        if (weaponMount != null)
        {
            targetParent = weaponMount;
        }

        foreach (var data in actionProps)
        {
            if (data.propObject != null)
            {
                GameObject propInstance = Instantiate(data.propObject);
                propInstance.name = data.propObject.name;

                // ★ [수정] 찾은 Weapon_R(혹은 오른손)을 부모로 설정
                propInstance.transform.SetParent(targetParent);

                // 위치/회전 초기화 (Weapon_R 기준으로 0,0,0 정렬)
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
            PrisonerAIType.Escaping => 7,
            PrisonerAIType.Graffiti => 8,
            PrisonerAIType.Ambusher => 9,
            PrisonerAIType.Digging => 10,
            PrisonerAIType.Suss => 11,
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

        // [수정] 이번 공격 프레임에서 이미 피격된 Health 컴포넌트를 기록할 리스트 생성
        HashSet<Health> damagedTargets = new HashSet<Health>();

        for (int i = 0; i < count; i++)
        {
            var target = hits[i];
            if (target.gameObject == gameObject) continue;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            dirToTarget.y = 0;
            Vector3 myForward = transform.forward;
            myForward.y = 0;

            // 각도 체크 (90도)
            if (Vector3.Angle(myForward, dirToTarget) < 90f) // attackAngle 변수 사용 권장 (현재는 90f 하드코딩 되어있음)
            {
                var playerHealth = target.GetComponent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInParent<Health>();
                if (playerHealth == null) playerHealth = target.GetComponentInChildren<Health>();

                // [수정] 유효한 Health가 있고, 아직 이번 공격에 맞지 않았다면 데미지 적용
                if (playerHealth != null && !damagedTargets.Contains(playerHealth))
                {
                    int finalDamage = (Data != null && Data.AttackPower > 0) ? (int)Data.AttackPower : 10;

                    playerHealth.TakeDamage(finalDamage);

                    // [수정] 피격 목록에 추가하여 중복 데미지 방지
                    damagedTargets.Add(playerHealth);

                    Debug.Log($"✅ [Hit Success] {name} -> Player ({finalDamage} dmg)");
                }
            }
        }
    }

    public void PlayAttackSound()
    {
        if (sfx != null)
        {
            sfx.PlayRandomAttack();
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