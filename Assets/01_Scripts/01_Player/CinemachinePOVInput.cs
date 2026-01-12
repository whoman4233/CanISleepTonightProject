using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachinePOVInput : MonoBehaviour
{
    private const float DefaultSensitivity = 0.1f;
    private const float DefaultCameraTransitionSpeed = 8f;

    [SerializeField] private Player player; // PlayerController -> Player

    [Header("Look Sensitivity")]
    [SerializeField] private float horizontalSensitivity = DefaultSensitivity;
    [SerializeField] private float verticalSensitivity = DefaultSensitivity;

    [Header("Camera Height (Crouch)")]
    [SerializeField] private float standingCameraOffsetY = 0.0f;
    [SerializeField] private float crouchingCameraOffsetY = -0.6f;
    [SerializeField] private float cameraTransitionSpeed = DefaultCameraTransitionSpeed;

    private CinemachineVirtualCamera vcam;
    private CinemachinePOV pov;

    private CinemachineFramingTransposer framingTransposer;
    private CinemachineTransposer transposer;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        pov = vcam.GetCinemachineComponent<CinemachinePOV>();

        // Body는 프로젝트 설정에 따라 FramingTransposer거나 Transposer일 수 있어서 둘 다 시도
        framingTransposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (pov == null || player == null) return;

        // Player가 PlayerInputs에서 읽어 캐싱해둔 LookInput 사용
        Vector2 look = player.LookInput;

        // 1) 좌우(Yaw): 플레이어 루트 회전
        float yawDelta = look.x * horizontalSensitivity;
        player.transform.Rotate(Vector3.up, yawDelta, Space.World);

        // 2) 상하(Pitch): POV Vertical만 사용
        pov.m_HorizontalAxis.m_InputAxisValue = 0f;
        pov.m_VerticalAxis.m_InputAxisValue = look.y * verticalSensitivity;
    }
    private void LateUpdate()
    {
        if (player == null) return;

        // 앉은 자세 유지 중이면 내려가게
        // (전환 중에도 같이 내려가고 싶으면 || player.IsCrouchTransitioning 추가)
        bool isCrouching = player.IsCrouchMode;

        float targetY = isCrouching ? crouchingCameraOffsetY : standingCameraOffsetY;

        // FramingTransposer 사용 중이면 TrackedObjectOffset, Transposer면 FollowOffset 조절
        if (framingTransposer != null)
        {
            Vector3 offset = framingTransposer.m_TrackedObjectOffset;
            offset.y = Mathf.Lerp(offset.y, targetY, Time.deltaTime * cameraTransitionSpeed);
            framingTransposer.m_TrackedObjectOffset = offset;
            return;
        }

        if (transposer != null)
        {
            Vector3 offset = transposer.m_FollowOffset;
            offset.y = Mathf.Lerp(offset.y, targetY, Time.deltaTime * cameraTransitionSpeed);
            transposer.m_FollowOffset = offset;
        }
    }
}