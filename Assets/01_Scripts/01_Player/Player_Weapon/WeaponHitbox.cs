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
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0)
            return;

        // Prisoner의 RagdollSetting 찾기(자식 콜라이더를 맞춰도 부모에서 찾음)
        RagdollSetting ragdoll = other.GetComponentInParent<RagdollSetting>();
        if (ragdoll == null)
            return;

        int id = ragdoll.gameObject.GetInstanceID();
        if (_hitTargets.Contains(id))
            return;

        _hitTargets.Add(id);

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        Vector3 dir = (ragdoll.transform.position - ownerRoot.position);
        if (dir.sqrMagnitude < 0.0001f)
            dir = ownerRoot.forward;

        ragdoll.ApplyImpact(hitPoint, dir.normalized, impactStrength);
    }
}