using UnityEngine;
using Cinemachine;

/// <summary>
/// Cinemachine Virtual Camera의 Noise(Perlin)를 이용한 카메라 쉐이크 컨트롤러
/// - Transform 직접 조작 금지
/// - Animator / Head Follow / QTE 연출과 충돌 없음
/// </summary>
public class CameraShakeController : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("Base Shake (QTE 지속 흔들림)")]
    [SerializeField] private float baseAmplitude = 0.12f;
    [SerializeField] private float baseFrequency = 10f;

    [Header("Impulse Shake (버튼 입력)")]
    [SerializeField] private float impulseAmplitude = 0.25f;
    [SerializeField] private float impulseDuration = 0.08f;

    [Header("Hit Impulse Shake (맞았을 때 흔들림")]
    [SerializeField] private float hitAmplitude = 0.6f;
    [SerializeField] private float hitFrequency = 10f;
    [SerializeField] private float hitDuration = 0.15f;


    private CinemachineBasicMultiChannelPerlin _perlin;
    private float _defaultAmplitude;
    private float _defaultFrequency;
    private bool _qteActive;

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("[CameraShakeController] CinemachineVirtualCamera 찾을 수 없음.");
            return;
        }
        Debug.Log(
$"[CameraShake] Target VCam = {virtualCamera.name}, Priority = {virtualCamera.Priority}");

_perlin = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (_perlin == null)
        {
            Debug.LogError(
                "[CameraShakeController] CinemachineBasicMultiChannelPerlin 찾을수 없음.\n" +
                "Virtual Camera의 Noise 슬롯에 Basic Multi Channel Perlin을 추가하세요."
            );
            return;
        }

        // 초기값 저장
        _defaultAmplitude = _perlin.m_AmplitudeGain;
        _defaultFrequency = _perlin.m_FrequencyGain;

        ResetAll();
    }
    public void OnQTEStarted()
    {
        _qteActive = true;
    }

    public void OnQTEEnded()
    {
        _qteActive = false;
        ResetAll();
    }

    // ======================================================
    // QTE 지속 흔들림
    // ======================================================

    /// <summary>
    /// QTE 중 죄수에게 붙잡힌 상태에서의 지속 흔들림
    /// </summary>
    public void StartBaseShake()
    {
        if (_perlin == null || !_qteActive)
            return;

        _perlin.m_AmplitudeGain = baseAmplitude;
        _perlin.m_FrequencyGain = baseFrequency;
    }

    /// <summary>
    /// 죄수 공격 종료 / QTE 종료 시 호출
    /// </summary>
    public void StopBaseShake()
    {
        if (_perlin == null)
            return;

        CancelInvoke(nameof(ResetImpulse));

        _perlin.m_AmplitudeGain = 0f;
    }

    // ======================================================
    // 버튼 입력 임펄스 쉐이크
    // ======================================================

    /// <summary>
    /// QTE 버튼 입력 시 순간적으로 튀는 흔들림
    /// </summary>
    public void PlayButtonImpulse()
    {
        if (_perlin == null || !_qteActive)
            return;

        _perlin.m_FrequencyGain = baseFrequency;
        // 순간적으로 진폭 증가
        _perlin.m_AmplitudeGain = impulseAmplitude;

        // 기존 Invoke 제거 후 재설정
        CancelInvoke(nameof(ResetImpulse));
        Invoke(nameof(ResetImpulse), impulseDuration);
    }

    private void ResetImpulse()
    {
        if (_perlin == null)
            return;

        if (!_qteActive)
            return;

        _perlin.m_FrequencyGain = baseFrequency;
        // 다시 기본 흔들림 상태로 복귀
        _perlin.m_AmplitudeGain = baseAmplitude;
    }

    // ======================================================
    // 강제 리셋
    // ======================================================

    /// <summary>
    /// QTE 종료, 씬 전환 등 모든 상황에서 안전하게 초기화
    /// </summary>
    public void ResetAll()
    {
        if (_perlin == null)
            return;

        // 예약된 임펄스 취소
        CancelInvoke(nameof(ResetImpulse));

        _perlin.m_AmplitudeGain = 0f;
        _perlin.m_FrequencyGain = _defaultFrequency;
    }

    // ======================================================
    // 일반 피격 전용 쉐이크 (QTE 무관)
    // ======================================================

    public void PlayHitImpulse()
    {
        if (_perlin == null || _qteActive)
            return;

        _perlin.m_FrequencyGain = hitFrequency;
        _perlin.m_AmplitudeGain = hitAmplitude;

        CancelInvoke(nameof(ResetHitImpulse));
        Invoke(nameof(ResetHitImpulse), hitDuration);
    }

    private void ResetHitImpulse()
    {
        if (_perlin == null)
            return;

        // QTE 중이면 기본 쉐이크 유지
        if (_qteActive)
            return;

        _perlin.m_AmplitudeGain = 0f;
    }
}


