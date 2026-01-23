using UnityEngine;

/// <summary>
/// 1인칭 카메라 높이 제어
/// - 입력에 즉시 반응
/// - 애니메이션 결과를 기다리지 않음
/// </summary>
public class FirstPersonCameraHeightController : MonoBehaviour
{
    [SerializeField] private Player player;

    [Header("Camera Height")]
    [SerializeField] private float standY = 1.8f;
    [SerializeField] private float crouchY = 1.0f;
    [SerializeField] private float transitionSpeed = 20f;

    private Vector3 _localPos;
    private bool _cameraCrouched;

    private void Awake()
    {
        _localPos = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        // -----------------------------------------
        // 1. 입력/전환/상태 중 하나라도 앉기면
        //    카메라는 즉시 내려간다
        // -----------------------------------------
        if (player.CrouchToggleRequested ||
            player.IsCrouchTransitioning ||
            player.IsCrouchMode)
        {
            _cameraCrouched = true;
        }

        // -----------------------------------------
        // 2. 완전히 서기 상태가 되었을 때만 복귀
        // -----------------------------------------
        if (!player.IsCrouchTransitioning &&
            !player.IsCrouchMode &&
            !player.CrouchToggleRequested)
        {
            _cameraCrouched = false;
        }

        float targetY = _cameraCrouched ? crouchY : standY;

        _localPos.y = Mathf.Lerp(
            _localPos.y,
            targetY,
            Time.deltaTime * transitionSpeed
        );

        transform.localPosition = _localPos;
    }
}
