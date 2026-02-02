using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public sealed class CinemachinePOVInput : MonoBehaviour
{
    // ===== 감도 범위(실사용 영역만) =====
    private const float MinSensitivity = 0.03f;   // 최저
    private const float MaxSensitivity = 0.25f;   // 최고(너무 빠르면 0.18~0.22로 낮춰)

    // ===== 곡선 세기(클수록 '최소 근처' 조절이 촘촘해짐) =====
    private const float SensitivityCurvePower = 3.0f;

    [SerializeField] private Player player;

    [Header("Look Sensitivity (Runtime)")]
    [SerializeField] private float horizontalSensitivity = MinSensitivity;
    [SerializeField] private float verticalSensitivity = MinSensitivity;

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

        Vector2 look = player.LookInput;

        // Yaw: 플레이어 회전
        float yawDelta = look.x * horizontalSensitivity;
        player.transform.Rotate(Vector3.up, yawDelta, Space.World);

        // Pitch: POV 입력
        pov.m_HorizontalAxis.m_InputAxisValue = 0f;
        pov.m_VerticalAxis.m_InputAxisValue = look.y * verticalSensitivity;
    }

    /// <summary>
    /// 설정창 슬라이더(0~1) 값을 실제 감도로 변환해서 적용
    /// </summary>
    public void SetLookSensitivityFromSlider(float slider01)
    {
        float t = Mathf.Clamp01(slider01);

        // 핵심: 초반을 촘촘하게 만들기
        float curved = Mathf.Pow(t, SensitivityCurvePower);

        // 실감도 계산
        float sensitivity = Mathf.Lerp(MinSensitivity, MaxSensitivity, curved);

        horizontalSensitivity = sensitivity;
        verticalSensitivity = sensitivity;
    }

    // (선택) 디버그/표시용
    public float GetCurrentSensitivity() => horizontalSensitivity;
}