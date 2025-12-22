using UnityEngine;

public class PrisonerActor : MonoBehaviour
{
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
    [SerializeField] private Animator animator; // 애니메이션 피드백용

    public void Init(string cellId, string instanceId, PrisonerDefinition def)
    {
        CellId = cellId;
        InstanceId = instanceId;

        Type = def.type;
        Hp = def.hp;
        Atk = def.atk;
        Spd = def.spd;

        // ✅ 스폰 직후 절대 전투 금지
        SetCombatEnabled(false);
    }

    public bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!IsAlive) return false;

        if (!_combatEnabled)
        {
            Debug.Log($"[Prisoner] First hit! Enabling combat for {InstanceId}");
            SetCombatEnabled(true);
        }

        Hp -= dmg;

        // 1. 이벤트 버스 전파 (UI나 사운드 매니저가 들음)
        PrisonerEventBus.RaisePrisonerHit(InstanceId, dmg);

        Debug.Log("Hit");

        if (Hp <= 0)
        {
            Hp = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
            // 2. 살아있을 때의 피격 연출
            PlayHitAnimation();
            // 필요하다면 살아있을 때도 약간의 물리 충격을 줄 수 있음
            // ragdoll.ApplyImpact(...)를 여기서 쓰면 즉시 쓰러지므로 주의
        }

        return true;
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        PrisonerEventBus.RaisePrisonerDown(InstanceId);

        // 기획서 반영: SetActive(false) 대신 래그돌 활성화
        if (ragdoll != null)
        {
            // 쓰러질 때의 강한 충격량 전달
            ragdoll.ApplyImpact(hitPoint, hitDirection, 10f);
        }
        else
        {
            gameObject.SetActive(false); // 래그돌 없으면 그냥 사라짐(백업)
        }

        Debug.Log($"[Prisoner] {InstanceId} has been suppressed.");
    }

    private void PlayHitAnimation()
    {
        if (animator == null) return;

        // 반항형/순응형에 따른 애니메이션 파라미터 분기 가능
        // 기획서: 순응형은 웅크리기, 반항형은 얼굴 가리기
        animator.SetTrigger("Hit");
    }

    public void SetCombatEnabled(bool enabled)
    {
        _combatEnabled = enabled;

        // ✅ Bad AI는 "전투 중 + Bad"일 때만 켜짐
        var badAi = GetComponent<PrisonerBadAI>();
        if (badAi != null)
            badAi.enabled = enabled && (Type == PrisonerType.Bad);
    }
}
