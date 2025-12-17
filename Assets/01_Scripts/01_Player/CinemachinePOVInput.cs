using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachinePOVInput : MonoBehaviour
{
    private const float DefaultSensitivity = 0.1f;

    [SerializeField] private Player player; // PlayerController -> Player

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
}