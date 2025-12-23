using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponHitbox : MonoBehaviour
{
    private static class LayerNames
    {
        public const string Prisoner = "Prisoner";
    }

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;

    private readonly HashSet<int> _hitTargets = new HashSet<int>();
    private int _prisonerLayer;

    // ✅ 스윙 활성 상태(디버깅/안전용)
    private bool _swingActive;

    private void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;

        _prisonerLayer = LayerMask.NameToLayer(LayerNames.Prisoner);

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // ✅ 히트박스 ON(스윙 시작) 때마다 호출
    public void BeginSwing()
    {
        _swingActive = true;
        _hitTargets.Clear();
    }

    // ✅ 히트박스 OFF(스윙 종료) 때 호출
    public void EndSwing()
    {
        _swingActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_swingActive) return;
        if (other == null) return;

        if (other.gameObject.layer != _prisonerLayer)
            return;

        int id = other.GetInstanceID();
        if (_hitTargets.Contains(id))
            return;

        _hitTargets.Add(id);

        if (!other.TryGetComponent<PrisonerActor>(out var prisoner))
            return;

        int damage = 10;

        var player = ownerRoot != null ? ownerRoot.GetComponent<Player>() : null;
        if (player != null && player.Data != null && player.Data.AttakData != null &&
            player.Data.AttakData.AttackInfoDatas != null && player.Data.AttakData.AttackInfoDatas.Count > 0)
        {
            damage = Mathf.Max(1, (int)player.Data.AttakData.AttackInfoDatas[0].Force);
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (other.transform.position - transform.position).normalized;

        prisoner.ApplyDamage(damage, hitPoint, hitDir);

        Debug.Log($"[WeaponHitbox] Hit Prisoner: {other.name}, dmg={damage}");
    }
}