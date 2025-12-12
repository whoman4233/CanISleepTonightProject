using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachinePOVInput : MonoBehaviour
{
    private const float DefaultSensitivity = 1f;

    [Header("References")]
    [SerializeField] private PlayerController playerController; // 플레이어에 붙은 스크립트

    [Header("Sensitivity")]
    [SerializeField] private float horizontalSensitivity = DefaultSensitivity;
    [SerializeField] private float verticalSensitivity = DefaultSensitivity;

    private CinemachineVirtualCamera virtualCamera;
    private CinemachinePOV pov;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    private void Update()
    {
        if (pov == null || playerController == null)
        {
            return;
        }

        // Input System 에서 Look 값 읽기 (Vector2)
        Vector2 lookInput = playerController.playerActions.Look.ReadValue<Vector2>();

        // POV의 입력값 채워주기
        // (Input Axis Name 은 비워두고, 이 값을 직접 넣어주는 방식)
        pov.m_HorizontalAxis.m_InputAxisValue = lookInput.x * horizontalSensitivity;
        pov.m_VerticalAxis.m_InputAxisValue = lookInput.y * verticalSensitivity;
    }
}