using UnityEngine;

public class Day : MonoBehaviour
{
    // ▶ 코드에서만 설정하는 값들
    private const float FULL_DAY_LENGTH = 360f;  // 하루 길이(초)
    private const float START_TIME = 0.5f;   // 시작 시간(0~1)

    [Range(0.0f, 1.0f)]
    public float time;
    private float timeRate;

    // 필요하면 이것도 코드에서 고정 가능
    public Vector3 noon = new Vector3(90f, 0f, 0f);

    [Header("Sun")]
    public Light sun;
    public Gradient sunColor;
    public AnimationCurve sunlntensity;

    [Header("Moon")]
    public Light moon;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Other Lighting")]
    public AnimationCurve lightingIntensityMultiplier;
    public AnimationCurve reflectionIntensityMultiplier;

    [Header("Skybox Blend")]
    [Tooltip("프리셋 바꿀 때 블렌딩에 걸리는 시간(초)")]
    public float skyBlendDuration = 5f;
    public float currentHour => time * 24;

    void Start()
    {
        // 상수로부터 직접 세팅
        timeRate = 1.0f / FULL_DAY_LENGTH;
        time = START_TIME;
    }

    void Update()
    {
        time = (time + timeRate * Time.deltaTime) % 1.0f;

        float sunRot = time * 360f - 90f;
        float moonRot = time * 360f + 90f;
        sun.transform.rotation = Quaternion.Euler(sunRot, 0f, 0f);
        moon.transform.rotation = Quaternion.Euler(moonRot, 0f, 0f);

        UpdateLighting(sun, sunColor, sunlntensity);
        UpdateLighting(moon, moonColor, moonIntensity);

        //(원하면 다시 사용)
        RenderSettings.ambientIntensity = lightingIntensityMultiplier.Evaluate(time);
        RenderSettings.reflectionIntensity = reflectionIntensityMultiplier.Evaluate(time);
    }

    void UpdateLighting(Light lightSource, Gradient gradient, AnimationCurve intensityCurve)
    {
        float intensity = intensityCurve.Evaluate(time);

        lightSource.color = gradient.Evaluate(time);
        lightSource.intensity = intensity;

        GameObject go = lightSource.gameObject;
        if (intensity == 0 && go.activeInHierarchy)
            go.SetActive(false);
        else if (intensity > 0 && !go.activeInHierarchy)
            go.SetActive(true);
    }
}