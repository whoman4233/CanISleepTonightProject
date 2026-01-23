using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponHitbox : MonoBehaviour
{
    private static class LayerNames
    {
        public const string Prisoner = "Prisoner";
    }

    private static class Defaults
    {
        public const int FallbackDamage = 1;
        public const int DefaultAttackIndex = 0;
    }

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;

    // 콜라이더를 끄기 위해 참조 변수 추가
    private Collider _weaponCollider;
    private int _prisonerLayer;

    private bool _swingActive;

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;

        _prisonerLayer = LayerMask.NameToLayer(LayerNames.Prisoner);

        if (_prisonerLayer == -1)
            Debug.LogError("[WeaponHitbox] 프로젝트 세팅에 'Prisoner' 레이어가 없습니다!");

        _weaponCollider = GetComponent<Collider>();
        if (_weaponCollider != null)
        {
            _weaponCollider.isTrigger = true;
            _weaponCollider.enabled = false; // 시작할 땐 꺼둠 (평상시 피격 방지)
        }
        else
        {
            Debug.LogError("[WeaponHitbox] Collider가 없습니다!");
        }
    }

    // ========================================================================
    // 애니메이션 이벤트에서 호출
    // ========================================================================
    public void BeginSwing()
    {
        _swingActive = true;

        // ★ [핵심 1] 공격 시작할 때만 콜라이더를 켬
        if (_weaponCollider != null)
            _weaponCollider.enabled = true;

        Debug.Log("[WeaponHitbox] BeginSwing - Collider ON");
    }

    public void EndSwing()
    {
        _swingActive = false;

        // ★ [핵심 2] 공격 끝나면 콜라이더를 끔 (헛스윙 했을 경우 대비)
        if (_weaponCollider != null)
            _weaponCollider.enabled = false;
    }

    // ========================================================================
    // 충돌 감지
    // ========================================================================
    private void OnTriggerEnter(Collider other)
    {
        // 스윙 중이 아니거나, 콜라이더가 이미 꺼졌다면 무시
        if (!_swingActive || other == null) return;

        // 1. 레이어 체크
        if (other.gameObject.layer != _prisonerLayer) return;

        // 2. 죄수 컴포넌트 찾기 (자식 -> 부모 순)
        var prisoner = other.GetComponent<PrisonerController>();
        if (prisoner == null)
        {
            prisoner = other.GetComponentInParent<PrisonerController>();
        }

        if (prisoner == null) return;

        // -----------------------------------------------------------
        // 3. 데미지 적용 및 콜라이더 즉시 종료
        // -----------------------------------------------------------

        // 데미지 계산
        int damage = GetPlayerDamage();
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (other.transform.position - transform.position).normalized;

        // 데미지 전달
        prisoner.ApplyDamage(damage, hitPoint, hitDir);
        Debug.Log($"[WeaponHitbox] Hit Success: {prisoner.name} (Dmg: {damage})");

        // ★ [핵심 3] 한 명 때렸으면 바로 콜라이더를 꺼버림! (다단히트 원천 차단)
        // 광역 공격(한 번에 두 명 베기)이 필요하다면 이 줄만 지우면 됩니다.
        if (_weaponCollider != null)
        {
            _weaponCollider.enabled = false;
            // Debug.Log("[WeaponHitbox] 1회 타격 완료 -> Collider OFF");
        }
    }

    private int GetPlayerDamage()
    {
        var player = ownerRoot != null ? ownerRoot.GetComponent<Player>() : null;
        if (player == null || player.Data == null)
            return Defaults.FallbackDamage;

        var attackData = player.Data.AttakData;
        if (attackData == null || attackData.AttackInfoDatas == null || attackData.AttackInfoDatas.Count == 0)
            return Defaults.FallbackDamage;

        int index = Mathf.Clamp(Defaults.DefaultAttackIndex, 0, attackData.AttackInfoDatas.Count - 1);
        int dmg = attackData.AttackInfoDatas[index].Damage;

        return Mathf.Max(1, dmg);
    }
}