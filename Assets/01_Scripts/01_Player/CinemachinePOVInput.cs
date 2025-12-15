using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachinePOVInput : MonoBehaviour
{
    private const float DefaultSensitivity = 0.1f;

    [SerializeField] private PlayerController playerController;

    [SerializeField] private float horizontalSensitivity = DefaultSensitivity;
    [SerializeField] private float verticalSensitivity = DefaultSensitivity;

    private CinemachineVirtualCamera vcam;
    private CinemachinePOV pov;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        pov = vcam.GetCinemachineComponent<CinemachinePOV>();

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (pov == null || playerController == null) return;

        Vector2 look = playerController.playerActions.Look.ReadValue<Vector2>();

        // 1) 좌우(Yaw): Player 루트 회전 (무한)
        float yawDelta = look.x * horizontalSensitivity;
        playerController.transform.Rotate(Vector3.up, yawDelta, Space.World);

        // 2) 상하(Pitch): POV Vertical로만
        pov.m_HorizontalAxis.m_InputAxisValue = 0f; // 수평 POV 비활성화
        pov.m_VerticalAxis.m_InputAxisValue = look.y * verticalSensitivity;
    }
}