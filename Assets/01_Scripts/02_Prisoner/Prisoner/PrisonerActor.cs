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

    private bool _initialized;

    private void Awake()
    {
        // ✅ 씬에 그냥 배치된 경우 Init이 안 올 수 있으니 폴백 초기화
        if (useSceneFallback && !_initialized)
            EnsureInitialized();
    }

    public void Init(string cellId, string instanceId, PrisonerDefinition def)
    {
        _initialized = true;

        CellId = cellId;
        InstanceId = instanceId;

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
        // ✅ Init이 안 왔어도 맞는 순간에는 최소 초기화
        if (!_initialized && useSceneFallback)
            EnsureInitialized();

        if (debugHit)
        {
            Debug.Log($"[PrisonerActor] ApplyDamage ENTER: dmg={dmg}, hp={Hp}, alive={IsAlive}, combat={_combatEnabled} hitPoint={hitPoint}", this);
        }

        if (!IsAlive)
            return false;

        if (!_combatEnabled)
        {
            Debug.Log($"[Prisoner] First hit! Enabling combat for {InstanceId}");
            SetCombatEnabled(true);
        }

        Hp -= dmg;

        PrisonerEventBus.RaisePrisonerHit(InstanceId, dmg);

        if (Hp <= 0)
        {
            Hp = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
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