using UnityEngine;
using Cinemachine;

/// <summary>
/// 1인칭 카메라 POV 입력 처리
/// - 위치 / 높이 / 클리핑은 절대 처리하지 않음
/// - 회전(Yaw, Pitch)만 담당
/// - QTE / CameraDirector와 충돌하지 않는 구조
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachinePOVInput : MonoBehaviour
{
    private const float DefaultSensitivity = 0.1f;

    [SerializeField] private Player player;

    [Header("Look Sensitivity")]
    [SerializeField] private float horizontalSensitivity = DefaultSensitivity;
    [SerializeField] private float verticalSensitivity = DefaultSensitivity;

    private CinemachineVirtualCamera vcam;
    private CinemachinePOV pov;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        pov = vcam.GetCinemachineComponent<CinemachinePOV>();

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (pov == null || player == null)
            return;

        // PlayerInputs에서 캐싱해 둔 Look 입력
        Vector2 look = player.LookInput;

        // =====================================================
        // 1) 좌우 회전 (Yaw)
        // - 플레이어 Root 회전
        // - QTE 시 CameraDirector가 이 값을 덮어씀
        // =====================================================
        float yawDelta = look.x * horizontalSensitivity;
        player.transform.Rotate(Vector3.up, yawDelta, Space.World);

        // =====================================================
        // 2) 상하 회전 (Pitch)
        // - Cinemachine POV Vertical Axis만 사용
        // - Horizontal Axis는 직접 쓰지 않음
        // =====================================================
        pov.m_HorizontalAxis.m_InputAxisValue = 0f;
        pov.m_VerticalAxis.m_InputAxisValue = look.y * verticalSensitivity;
    }
}
