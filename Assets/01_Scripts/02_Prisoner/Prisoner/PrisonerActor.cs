using UnityEngine;

public class PrisonerActor : MonoBehaviour
{
    private static class Defaults
    {
        public const int Hp = 3;
        public const int Atk = 1;
        public const int Spd = 1;
    }

    public string InstanceId { get; private set; }
    public string CellId { get; private set; }
    public PrisonerType Type { get; private set; }

    public int Hp { get; private set; }
    public int Atk { get; private set; }
    public int Spd { get; private set; }

    public bool IsAlive => Hp > 0;
    public bool CombatEnabled => _combatEnabled;

    private bool _combatEnabled;

    private PrisonerFSM fsm; 
    
    public bool IsSuspicious { get; private set; }

    [Header("Feedback Refs")]
    [SerializeField] private RagdollSetting ragdoll;
    [SerializeField] private Animator animator;

    [Header("Scene Fallback (Init 미호출 대비)")]
    [SerializeField] private bool useSceneFallback = true;
    [SerializeField] private int fallbackHp = Defaults.Hp;
    [SerializeField] private int fallbackAtk = Defaults.Atk;
    [SerializeField] private int fallbackSpd = Defaults.Spd;
    [SerializeField] private PrisonerType fallbackType = PrisonerType.Bad;

    [Header("Debug (Minimal)")]
    [SerializeField] private bool debugHit;

    [SerializeField] private PrisonerSfxController sfx;

    private bool _initialized;

    private void Awake()
    {
        // ✅ 씬에 그냥 배치된 경우 Init이 안 올 수 있으니 폴백 초기화
        if (useSceneFallback && !_initialized)
            EnsureInitialized(); 
        fsm = GetComponent<PrisonerFSM>();
        fsm.actor = this;

        if (sfx == null)
            sfx = GetComponent<PrisonerSfxController>();
    }

    public void Init(string cellId, string instanceId, PrisonerDefinition def, bool isSuspicious)
    {
        _initialized = true;

        CellId = cellId;
        InstanceId = instanceId; 
        this.IsSuspicious =  isSuspicious;

        Type = def.type;
        Hp = def.hp;
        Atk = def.atk;
        Spd = def.spd;

        SetCombatEnabled(false);

        if (debugHit)
            Debug.Log($"[PrisonerActor] Init: id={InstanceId}, hp={Hp}", this);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        _initialized = true;

        // InstanceId가 비어있으면 임시로 유니크하게 부여(디버그용)
        if (string.IsNullOrEmpty(InstanceId))
            InstanceId = gameObject.name;

        Type = fallbackType;
        Hp = Mathf.Max(1, fallbackHp);
        Atk = Mathf.Max(1, fallbackAtk);
        Spd = Mathf.Max(1, fallbackSpd);

        SetCombatEnabled(false);

        if (debugHit)
            Debug.Log($"[PrisonerActor] Fallback Init: id={InstanceId}, hp={Hp}", this);
    }

    public bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!IsAlive) return false;

        // ✅ 핵심 1: Idle 상태일 때는 공격 무시 (무적)
        if (fsm != null && fsm.IsInvulnerable)
        {
            Debug.Log($"[Prisoner] {InstanceId} is sitting Idle. Damage blocked.");
            return false;
        }

        // ✅ 핵심 2: 첫 피격 시 전투 모드 활성화 (기획: 팬다 -> 진압 시작)
        if (!_combatEnabled) SetCombatEnabled(true);

        Hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(InstanceId, dmg);

        if (Hp <= 0)
        {
            Hp = 0;
            fsm.ChangeState(fsm.DeadState); // FSM을 사망 상태로
            PrisonerEventBus.RaisePrisonerDown(InstanceId);

            if (sfx != null) sfx.PlayRandomDieOnce();
            Die(hitPoint, hitDirection);

            // 래그돌은 DeadState.Enter()에서 처리하거나 여기서 처리
            if (ragdoll != null) ragdoll.ApplyImpact(hitPoint, hitDirection, 10f);
        }
        else
        {
            // FSM에게 피격 알림 (전투/웅크리기 전환 트리거)
            fsm.OnDamaged(dmg, hitPoint, hitDirection);

            if (sfx != null) sfx.PlayHitAndRandomMoan();
            PlayHitAnimation();
        }

        return true;
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        PrisonerEventBus.RaisePrisonerDown(InstanceId);

        if (ragdoll != null)
        {
            const float DeathImpactStrength = 10f; // 매직넘버 방지 시 const로
            ragdoll.ApplyImpact(hitPoint, hitDirection, DeathImpactStrength);
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log($"[Prisoner] {InstanceId} has been suppressed.");
    }

    private void PlayHitAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger("Hit");
    }

    public void SetCombatEnabled(bool enabled)
    {
        _combatEnabled = enabled;

        var badAi = GetComponent<PrisonerBadAI>();
        if (badAi != null)
            badAi.enabled = enabled && (Type == PrisonerType.Bad);
    }
}