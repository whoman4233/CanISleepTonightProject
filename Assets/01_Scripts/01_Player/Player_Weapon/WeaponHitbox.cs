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
        public const float VfxAutoDestroySeconds = 2.0f; // 매직넘버 방지
    }

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;

    [Header("Hit VFX (Assign on Weapon Prefab)")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Tooltip("비워두면 이 오브젝트(WeaponHitbox) 위치 기준")]
    [SerializeField] private Transform vfxSpawnPoint;

    private Collider _weaponCollider;
    private int _prisonerLayer;
    private bool _swingActive;


    // 이번 스윙(공격)에서 이미 타격 판정이 완료된 객체들의 ID 저장
    private readonly HashSet<int> _alreadyHitList = new HashSet<int>();

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;

        if (vfxSpawnPoint == null)
            vfxSpawnPoint = transform;

        _prisonerLayer = LayerMask.NameToLayer(LayerNames.Prisoner);

        if (_prisonerLayer == -1)
            Debug.LogError("[WeaponHitbox] 프로젝트 세팅에 'Prisoner' 레이어가 없습니다!");

        _weaponCollider = GetComponent<Collider>();
        if (_weaponCollider != null)
        {
            _weaponCollider.isTrigger = true;
            _weaponCollider.enabled = false;
        }
        else
        {
            Debug.Log("[WeaponHitbox] Collider가 없습니다!");
        }
    }

    // 애니메이션 이벤트에서 호출
    public void BeginSwing()
    {
        _alreadyHitList.Clear(); // 새로운 공격 시작 시 명부 초기화
        _swingActive = true;
        if (_weaponCollider != null)
            _weaponCollider.enabled = true;
    }

    public void EndSwing()
    {
        _swingActive = false;
        if (_weaponCollider != null)
            _weaponCollider.enabled = false;
        _alreadyHitList.Clear(); // 안전을 위해 종료 시에도 비움
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_swingActive || other == null) return;
        if (other.gameObject.layer != _prisonerLayer) return;

        int targetId = other.GetInstanceID();
        if (_alreadyHitList.Contains(targetId)) return;

        var prisoner = other.GetComponent<PrisonerController>() ?? other.GetComponentInParent<PrisonerController>();
        if (prisoner == null) return;

        // 타격 성공 시 명부에 추가 (다단히트 방지)
        _alreadyHitList.Add(targetId);

        int damage = GetPlayerDamage();
        Vector3 hitPoint = other.ClosestPoint(vfxSpawnPoint.position);
        Vector3 hitDir = (other.transform.position - vfxSpawnPoint.position).normalized;

        prisoner.ApplyDamage(damage, hitPoint, hitDir);
        PlayHitVfx(hitPoint, hitDir);

        // 1회 타격 후 다단히트 방지(네 코드 유지)
        //if (_weaponCollider != null)
        //    _weaponCollider.enabled = false;

    }

    private void PlayHitVfx(Vector3 hitPoint, Vector3 hitDir)
    {
        if (hitVfxPrefab == null) return;

        // hitDir 방향으로 이펙트를 살짝 돌려주고 싶으면 사용
        Quaternion rot = hitDir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(hitDir) : Quaternion.identity;

        GameObject vfx = Instantiate(hitVfxPrefab, hitPoint, rot);
        Destroy(vfx, Defaults.VfxAutoDestroySeconds);
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