using UnityEngine;

public sealed class PlayerWeaponHandler : MonoBehaviour
{
    private static class LayerNames
    {
        public const string PlayerWeapon = "PlayerWeapon";
    }

    [Header("Equip")]
    [SerializeField] private Transform rightHandSocket;
    [SerializeField] private GameObject startingWeaponPrefab;

    private Collider _weaponHitCollider;
    private WeaponHitbox _weaponHitbox;
    private GameObject _equippedWeapon;

    public void EquipOnStart()
    {
        if (rightHandSocket == null || startingWeaponPrefab == null)
        {
            Debug.LogWarning("[PlayerWeaponHandler] Socket 또는 Starting Weapon이 비어있습니다.");
            return;
        }

        if (_equippedWeapon != null)
        {
            Destroy(_equippedWeapon);
            _equippedWeapon = null;
        }

        _equippedWeapon = Instantiate(startingWeaponPrefab, rightHandSocket);
        _equippedWeapon.transform.localPosition = Vector3.zero;
        _equippedWeapon.transform.localRotation = Quaternion.identity;
        _equippedWeapon.transform.localScale = Vector3.one;

        SetLayerRecursively(_equippedWeapon, LayerMask.NameToLayer(LayerNames.PlayerWeapon));

        _weaponHitbox = _equippedWeapon.GetComponentInChildren<WeaponHitbox>(true);
        _weaponHitCollider = _weaponHitbox != null ? _weaponHitbox.GetComponent<Collider>() : null;

        if (_weaponHitCollider != null)
            _weaponHitCollider.isTrigger = true;

        SetHitColliderEnabled(false);
    }

    public void SetHitColliderEnabled(bool enabled)
    {
        if (_weaponHitCollider == null)
        {
            if (_equippedWeapon != null)
            {
                _weaponHitbox = _equippedWeapon.GetComponentInChildren<WeaponHitbox>(true);
                _weaponHitCollider = _weaponHitbox != null ? _weaponHitbox.GetComponent<Collider>() : null;
                if (_weaponHitCollider != null) _weaponHitCollider.isTrigger = true;
            }
        }

        if (_weaponHitbox != null)
        {
            if (enabled) _weaponHitbox.BeginSwing();
            else _weaponHitbox.EndSwing();
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0) return;

        root.layer = layer;
        var t = root.transform;

        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}