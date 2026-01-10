using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Transform shakeRoot;

    [Header("기본 흔들림 (Continuous)")]
    [SerializeField] private float baseAmplitude = 0.06f; //지속 흔들림 값
    [SerializeField] private float baseFrequency = 18f; // 지속 흔들림 속도

    [Header("입력시 흔들림 (Button)")]
    [SerializeField] private float impulseStrength = 0.25f; // 버튼 입력 시 순간적으로 흔들림 강도
    [SerializeField] private float impulseDamping = 20f; // 흔들림에서 원래 위치로 돌아오는 속도

    private Vector3 _originLocalPos;

    // 상태값
    private bool _baseShakeActive;
    private Vector3 _impulseOffset;

    private float _time;

    private void Awake()
    {
        if (shakeRoot == null)
            shakeRoot = transform;

        _originLocalPos = shakeRoot.localPosition;
    }

    private void Update()
    {
        _time += Time.deltaTime;

        Vector3 baseOffset = Vector3.zero;

        // 지속 흔들림 (붙잡힘 상태)
        if (_baseShakeActive)
        {
            baseOffset.x = Mathf.Sin(_time * baseFrequency) * baseAmplitude;
            baseOffset.y = Mathf.Cos(_time * baseFrequency * 0.9f) * baseAmplitude;
        }

        // 버튼 임펄스 감쇠 (발버둥)
        _impulseOffset = Vector3.Lerp(
            _impulseOffset,
            Vector3.zero,
            impulseDamping * Time.deltaTime
        );

        shakeRoot.localPosition = _originLocalPos + baseOffset + _impulseOffset;
    }

    // =========================
    // Public API
    // =========================

    /// <summary>
    /// 죄수 공격 애니메이션 중 호출
    /// 죄수 공격 중 지속 흔들림 시작
    /// </summary>
    public void StartBaseShake()
    {
        _baseShakeActive = true;
    }

    /// <summary>
    /// 공격 애니메이션 종료 시 호출
    /// 죄수 공격 종료 시 호출
    /// </summary>
    public void StopBaseShake()
    {
        _baseShakeActive = false;
    }

    /// <summary>
    /// QTE 버튼 입력 시 호출
    /// </summary>
    public void PlayButtonImpulse()
    {
        // 랜덤 방향으로 순간 흔들림 누적
        _impulseOffset += Random.insideUnitSphere * impulseStrength;
    }

    /// <summary>
    /// QTE 종료 또는 강제 리셋 시 호출.
    /// 모든 흔들림 상태 초기화.
    /// </summary>
    public void ResetAll()
    {
        _baseShakeActive = false;
        _impulseOffset = Vector3.zero;
        shakeRoot.localPosition = _originLocalPos;
    }
}

