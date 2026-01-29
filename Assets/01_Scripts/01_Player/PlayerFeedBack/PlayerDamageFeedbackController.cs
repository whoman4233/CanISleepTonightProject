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

        // 프리팹이므로 씬 준비를 기다렸다가 바인딩
        StartCoroutine(Co_BindDamageVolume());
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onDamaged);
    }

    // =========================
    // Volume Binding
    // =========================
    private IEnumerator Co_BindDamageVolume()
    {
        // 씬 로딩 / 오브젝트 활성 순서 대비
        const float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var marker = FindObjectOfType<PlayerHitVolumeMarker>();
            if (marker != null)
            {
                _volume = marker.GetComponent<Volume>();
                if (_volume != null && _volume.profile.TryGet(out _vignette))
                {
                    _defaultIntensity = _vignette.intensity.value;
                    _volumeBound = true;

                    Debug.Log("[PlayerDamageFeedback] Damage Volume 바인딩 성공");
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogError("[PlayerDamageFeedback] Damage Volume 바인딩 실패 (타임아웃)");
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

