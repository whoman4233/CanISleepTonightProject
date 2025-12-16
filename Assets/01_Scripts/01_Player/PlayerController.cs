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
    //public void Initialize()
    //{
    //    //SetMovementEnabled(false);
    //}
    public void SetMovementEnabled(bool isEnabled)
    {
        if (playerInput == null)
        {
            Debug.LogError("playerInput이 Initialize되지 않았습니다. Bootstrap 순서 확인 필요.");
            return;
        }
        if (isEnabled)
        {
            playerInput.Enable();
            Debug.Log("PlayerController 입력 활성화");
        }
        else
        {
            playerInput.Disable();
            Debug.Log("PlayerController입력 비활성화");
        }
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
    private void Start()
    {
        pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        playerInput.Enable();
    }

    private void LateUpdate()
    {
        if (pov == null) return;

        // 카메라의 Yaw 값을 Player Y 회전에 그대로 사용
        float cameraYaw = pov.m_HorizontalAxis.Value;
        //transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
    }
}