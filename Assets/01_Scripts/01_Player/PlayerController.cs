using Cinemachine;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    public PlayerInputs playerInput { get; private set; }
    public PlayerInputs.PlayerActions playerActions { get; private set; }
    private CinemachinePOV pov;
    private void Awake()
    {
        playerInput = new PlayerInputs();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
    private void Start()
    {
        pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
    }

    private void LateUpdate()
    {
        if (pov == null) return;

        // 카메라의 Yaw 값을 Player Y 회전에 그대로 사용
        float cameraYaw = pov.m_HorizontalAxis.Value;
        //transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
    }
}