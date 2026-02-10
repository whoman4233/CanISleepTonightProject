using UnityEngine;

/// <summary>
/// 1인칭 카메라 높이 제어
/// - 입력(IsCrouching)에 즉시 반응
/// - 애니메이션/상태 종료를 기다리지 않음
/// </summary>
public class FirstPersonCameraHeightController : MonoBehaviour
{
    [SerializeField] private Player player;

    [Header("Camera Height")]
    [SerializeField] private float standY = 1.8f;
    [SerializeField] private float crouchY = 1.0f;
    [SerializeField] private float transitionSpeed = 16f;

    private Vector3 _localPos;

    private void Awake()
    {
        _localPos = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        bool cameraCrouched = player.IsCrouching;

        float targetY = cameraCrouched ? crouchY : standY;

        _localPos.y = Mathf.Lerp(
            _localPos.y,
            targetY,
            Time.deltaTime * transitionSpeed
        );

        transform.localPosition = _localPos;
    }
}
