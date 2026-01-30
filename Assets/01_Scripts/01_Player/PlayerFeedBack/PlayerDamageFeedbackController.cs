using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerDamageFeedbackController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CameraShakeController cameraShake;

    [Header("Vignette")]
    [SerializeField] private float vignetteIntensity = 0.4f;
    [SerializeField] private float vignetteDuration = 0.15f;

    [Header("SFX")]
    [SerializeField] private PlayerSfxController sfxController;

    // =========================
    // Runtime refs
    // =========================
    private Volume _volume;
    private Vignette _vignette;
    private float _defaultIntensity;

    private Action<PlayerDamagedEvent> _onDamaged;
    private bool _volumeBound;

    private Coroutine _bindRoutine;

    // =========================
    // Lifecycle
    // =========================
    private void Awake()
    {
        _onDamaged = OnPlayerDamaged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onDamaged);

        // 플레이어는 런타임 스폰이므로 씬 준비 이후 바인딩 시도
        if (_bindRoutine != null)
            StopCoroutine(_bindRoutine);

        _bindRoutine = StartCoroutine(Co_BindDamageVolume());
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onDamaged);

        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
    }

    // =========================
    // Volume Binding
    // =========================
    private IEnumerator Co_BindDamageVolume()
    {
        const float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var marker = FindObjectOfType<PlayerHitVolumeMarker>();
            if (marker != null)
            {
                _volume = marker.GetComponent<Volume>();
                if (_volume != null && _volume.profile != null &&
                    _volume.profile.TryGet(out _vignette))
                {
                    _defaultIntensity = _vignette.intensity.value;
                    _volumeBound = true;

                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Marker가 없으면 "이 씬에서는 비네팅 미사용"(튜토리얼 신)

        _volumeBound = false;
        _vignette = null;
    }

    // =========================
    // Event
    // =========================
    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        // 카메라 흔들림
        if (cameraShake != null)
            cameraShake.PlayHitImpulse();

        // 비네팅
        if (_volumeBound && _vignette != null)
            StartCoroutine(Co_Vignette());

        // 사운드
        if (sfxController != null)
            sfxController.PlayHitRandomSfx();
    }

    // =========================
    // Effects
    // =========================
    private IEnumerator Co_Vignette()
    {
        _vignette.intensity.value = vignetteIntensity;
        yield return new WaitForSeconds(vignetteDuration);
        _vignette.intensity.value = _defaultIntensity;
    }
}


