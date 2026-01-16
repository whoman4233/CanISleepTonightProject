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

    private readonly HashSet<int> _hitTargets = new HashSet<int>();
    private int _prisonerLayer;

    private bool _swingActive;

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;

        _prisonerLayer = LayerMask.NameToLayer(LayerNames.Prisoner);

        // [안전장치] 레이어 설정이 잘못되었을 경우 경고
        if (_prisonerLayer == -1)
            Debug.LogError("[WeaponHitbox] 프로젝트 세팅에 'Prisoner' 레이어가 없습니다! Add Layer를 해주세요.");

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public void BeginSwing()
    {
        _swingActive = true;
        _hitTargets.Clear();
        // [디버그] 공격 시작 신호 확인 (이 로그가 안 뜨면 애니메이션 이벤트 연결 끊김)
        Debug.Log("[WeaponHitbox] BeginSwing - Hitbox ON");
    }

    public void EndSwing()
    {
        _swingActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_swingActive) return;
        if (other == null) return;

        // [디버그] 무엇에 닿았는지 확인 (2일차에 이 로그조차 안 뜨면 Time.timeScale 문제)
        // Debug.Log($"[WeaponHitbox] Touch: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // 1. 레이어 체크
        if (other.gameObject.layer != _prisonerLayer)
        {
            // 죄수 레이어가 아니면 무시 (필요시 주석 해제하여 확인)
            // Debug.LogWarning($"[WeaponHitbox] Ignore: {other.name} is not Prisoner Layer.");
            return;
        }

        int id = other.GetInstanceID();
        if (_hitTargets.Contains(id))
            return;

        _hitTargets.Add(id);

        // 2. 죄수 컴포넌트 찾기 (자식 -> 부모 순)
        var prisoner = other.GetComponent<PrisonerController>();
        if (prisoner == null)
        {
            prisoner = other.GetComponentInParent<PrisonerController>();
        }

        // 여전히 없으면 리턴
        if (prisoner == null)
        {
            Debug.LogError($"[WeaponHitbox] 오류! '{other.name}'은 Prisoner 레이어지만 Controller가 없습니다.");
            return;
        }

        // 3. 데미지 적용
        int damage = GetPlayerDamage();
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (other.transform.position - transform.position).normalized;

        prisoner.ApplyDamage(damage, hitPoint, hitDir);

        Debug.Log($"[WeaponHitbox] Hit Prisoner Success: {prisoner.name} (Dmg: {damage})");
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