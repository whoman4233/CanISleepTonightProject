using UnityEngine;

public sealed class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Equip")]
    [SerializeField] private Transform rightHandSocket;
    [SerializeField] private GameObject startingWeaponPrefab;

    [Header("Hit (Optional)")]
    [SerializeField] private Collider weaponHitCollider;

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

        // 자동으로 콜라이더 찾기(할당 안 했으면)
        if (weaponHitCollider == null)
            weaponHitCollider = _equippedWeapon.GetComponentInChildren<Collider>(true);

        SetHitColliderEnabled(false);
    }

    public void SetHitColliderEnabled(bool enabled)
    {
        if (weaponHitCollider != null)
            weaponHitCollider.enabled = enabled;
    }
}