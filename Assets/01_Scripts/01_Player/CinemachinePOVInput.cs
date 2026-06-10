using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public sealed class CinemachinePOVInput : MonoBehaviour
{
    // 슬라이더 0일 때 "완전 정지"를 만들기 위해 Min은 0으로 둡니다.
    private const float MinSensitivity = 0.0f;

    // 1일 때 너무 빠르면 여기만 낮추면 됨 (프로젝트마다 체감 다름)
    // 추천 시작값: 0.16 ~ 0.20
    private const float MaxSensitivity = 0.18f;

    // 0 근처를 더 촘촘하게(미세 조절) 만들고 싶으면 키우기
    private const float SensitivityCurvePower = 2.5f;

    // 아주 작은 입력(노이즈)을 무시하는 데드존 (움직임 "미세하게 남는" 느낌 방지)
    private const float InputDeadZone = 0.0005f;

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

        // 입력 노이즈 제거 (특히 0 감도에서 미세하게 움직이는 느낌 방지)
        if (Mathf.Abs(look.x) < InputDeadZone) look.x = 0f;
        if (Mathf.Abs(look.y) < InputDeadZone) look.y = 0f;

        // Horizontal (Yaw): 플레이어 회전
        float yawDelta = look.x * horizontalSensitivity;
        if (yawDelta != 0f)
            player.transform.Rotate(Vector3.up, yawDelta, Space.World);

        // Vertical (Pitch): POV 입력
        pov.m_HorizontalAxis.m_InputAxisValue = 0f;
        pov.m_VerticalAxis.m_InputAxisValue = look.y * verticalSensitivity;
    }

    // ===== Public API =====

    // (호환용) 예전 1슬라이더: 둘 다 같은 값
    public void SetLookSensitivityFromSlider(float slider01)
    {
        float sens = Slider01ToSensitivity(slider01);
        horizontalSensitivity = sens;
        verticalSensitivity = sens;
    }

    public void SetHorizontalSensitivityFromSlider(float slider01)
    {
        horizontalSensitivity = Slider01ToSensitivity(slider01);
    }

    public void SetVerticalSensitivityFromSlider(float slider01)
    {
        verticalSensitivity = Slider01ToSensitivity(slider01);
    }

    public float GetHorizontalSensitivity() => horizontalSensitivity;
    public float GetVerticalSensitivity() => verticalSensitivity;

    private static float Slider01ToSensitivity(float slider01)
    {
        float t = Mathf.Clamp01(slider01);

        // 슬라이더 0은 확실하게 0으로 (부동소수점/곡선 영향 제거)
        if (t <= 0f)
            return 0f;

        float curved = Mathf.Pow(t, SensitivityCurvePower);
        return Mathf.Lerp(MinSensitivity, MaxSensitivity, curved);
    }
}