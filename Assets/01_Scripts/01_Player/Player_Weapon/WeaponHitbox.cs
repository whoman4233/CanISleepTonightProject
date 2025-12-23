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

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public void BeginSwing()
    {
        _swingActive = true;
        _hitTargets.Clear();
    }

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

        int damage = GetPlayerDamage();

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (other.transform.position - transform.position).normalized;

        prisoner.ApplyDamage(damage, hitPoint, hitDir);

        Debug.Log($"[WeaponHitbox] Hit Prisoner: {other.name}, dmg={damage}");
    }

    private int GetPlayerDamage()
    {
        // ✅ PlayerSO(=player.Data)에서 AttackInfoData.Damage를 읽음
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