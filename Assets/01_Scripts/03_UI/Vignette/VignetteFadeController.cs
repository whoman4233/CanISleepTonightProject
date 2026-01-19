using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteFadeController : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float fadeDuration = 0.5f;

    private Vignette _vignette;
    private Coroutine _routine;

    private void Awake()
    {
        if (volume == null || !volume.profile.TryGet(out _vignette))
        {
            Debug.LogError("[VignetteFade] Vignette not found in Volume Profile");
        }

        // 초기 상태
        SetIntensity(0f);
    }

    public Coroutine FadeOut(MonoBehaviour owner)
    {
        return StartFade(owner, 1f);
    }

    public Coroutine FadeIn(MonoBehaviour owner)
    {
        return StartFade(owner, 0f);
    }

    private Coroutine StartFade(MonoBehaviour owner, float target)
    {
        if (_routine != null)
            owner.StopCoroutine(_routine);

        _routine = owner.StartCoroutine(Co_Fade(target));
        return _routine;
    }

    private IEnumerator Co_Fade(float target)
    {
        float start = _vignette.intensity.value;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float value = Mathf.Lerp(start, target, t / fadeDuration);
            SetIntensity(value);
            yield return null;
        }

        SetIntensity(target);
        _routine = null;
    }

    private void SetIntensity(float value)
    {
        if (_vignette != null)
            _vignette.intensity.value = value;
    }
}
