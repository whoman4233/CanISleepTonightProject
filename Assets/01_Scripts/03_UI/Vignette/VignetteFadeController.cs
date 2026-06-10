using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteFadeController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;

    [Header("Exposure")]
    [Tooltip("암전 시 목표 Exposure 값 (보통 -10 ~ -15)")]
    [SerializeField] private float blackoutExposure = -12f;

    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    private Coroutine _routine;

    private void Awake()
    {
        if (volume == null)
            return;

        volume.weight = 1f;

        if (!volume.profile.TryGet(out _vignette))
        {
            return;
        }
        else
        {
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = 0f;
        }

        if (!volume.profile.TryGet(out _colorAdjustments))
        {
            return;
        }
        else
        {
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = 0f;
        }
    }

    // =========================
    // Public API
    // =========================

    /// <summary>
    /// 화면을 암전 상태로 페이드
    /// </summary>
    public Coroutine FadeOut(MonoBehaviour owner, float duration)
    {
        return StartFade(owner, targetVignette: 1f, targetExposure: blackoutExposure, duration);
    }

    /// <summary>
    /// 암전을 해제하며 화면 복구
    /// </summary>
    public Coroutine FadeIn(MonoBehaviour owner, float duration)
    {
        return StartFade(owner, targetVignette: 0f, targetExposure: 0f, duration);
    }

    // =========================
    // Internal
    // =========================

    private Coroutine StartFade(
        MonoBehaviour owner,
        float targetVignette,
        float targetExposure,
        float duration)
    {
        if (_routine != null)
            owner.StopCoroutine(_routine);

        _routine = owner.StartCoroutine(
            Co_Fade(targetVignette, targetExposure, duration)
        );
        return _routine;
    }

    private IEnumerator Co_Fade(
        float targetVignette,
        float targetExposure,
        float duration)
    {
        float startVignette = _vignette != null ? _vignette.intensity.value : 0f;
        float startExposure = _colorAdjustments != null ? _colorAdjustments.postExposure.value : 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            if (_vignette != null)
            {
                _vignette.intensity.value =
                    Mathf.Lerp(startVignette, targetVignette, lerp);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value =
                    Mathf.Lerp(startExposure, targetExposure, lerp);
            }

            yield return null;
        }

        if (_vignette != null)
            _vignette.intensity.value = targetVignette;

        if (_colorAdjustments != null)
            _colorAdjustments.postExposure.value = targetExposure;

        _routine = null;
    }
}

