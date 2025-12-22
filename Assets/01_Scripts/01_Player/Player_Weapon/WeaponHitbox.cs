using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponHitbox : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;   // Player 루트
    [SerializeField] private float impactStrength = 20f;

    [Header("Filters")]
    [SerializeField] private LayerMask hittableLayers;

    private readonly HashSet<int> _hitTargets = new HashSet<int>();

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;
    }

    private void OnEnable()
    {
        // HitboxOn 될 때마다 초기화 = 한 스윙 1회 판정 보장
        _hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 죄수인지 확인
        if (other.TryGetComponent<PrisonerActor>(out var prisoner))
        {
            // 2. PlayerSO에서 공격력 가져오기
            // 기획서상 PlayerSO에 AttackPower 혹은 유사한 필드가 있다고 가정합니다.
            var player = GetComponent<Player>();
            int damage = (player != null && player.Data != null) ? (int)player.Data.AttakData.AttackInfoDatas[0].Force : 10;

            // 3. 타격 지점 및 방향 계산 (물리 효과용)
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDir = (other.transform.position - transform.position).normalized;

            // 4. 죄수에게 데미지 전달
            prisoner.ApplyDamage(damage, hitPoint, hitDir);

            Debug.Log($"[Hit] {other.name} hit by player with {damage} dmg.");
        }
    }
}