using UnityEngine;

public class Skybox4Blender : MonoBehaviour
{
    [Header("Day 스크립트 연결")]
    public Day day;   // 이건 인스펙터에서 연결

    [Header("시간대별 Cubemap")]
    public Cubemap night;     // 0시 ~ 새벽
    public Cubemap sunrise;   // 아침
    public Cubemap daySky;    // 낮
    public Cubemap sunset;    // 저녁

    // ▶ 코드에서만 설정하는 값들
    // 4개 시간대 비율
    private const float NIGHT_PERCENT = 0.25f;
    private const float SUNRISE_PERCENT = 0.25f;
    private const float DAY_PERCENT = 0.25f;
    private const float SUNSET_PERCENT = 0.25f;

    // 블렌딩 강도, 경계 폭, 회전속도
    private const float BLEND_SHARPNESS = 1.5f;
    private const float EDGE = 0.1f; // 하루 비율
    private const float ROTATION_SPEED = 1f;

    Material skyMat;

    void Start()
    {
        skyMat = RenderSettings.skybox;
    }

    void Update()
    {
        if (day == null || skyMat == null) return;

        float t = Mathf.Repeat(day.time, 1f); // 0~1

        // 1) 네 구간의 "절대 시간" 경계 계산
        float total = NIGHT_PERCENT + SUNRISE_PERCENT + DAY_PERCENT + SUNSET_PERCENT;

        float nightEnd = NIGHT_PERCENT / total;
        float sunriseEnd = nightEnd + SUNRISE_PERCENT / total;
        float dayEnd = sunriseEnd + DAY_PERCENT / total;
        float sunsetEnd = 1f;

        float e = Mathf.Clamp(EDGE, 0f, 0.25f);

        Cubemap from = night;
        Cubemap to = night;
        float k = 0f;

        // --- Night ---
        if (t < nightEnd - e)
        {
            from = night; to = night; k = 0f;
        }
        else if (t < nightEnd + e)
        {
            from = night; to = sunrise;
            k = Mathf.InverseLerp(nightEnd - e, nightEnd + e, t);
        }
        // --- Sunrise ---
        else if (t < sunriseEnd - e)
        {
            from = sunrise; to = sunrise; k = 0f;
        }
        else if (t < sunriseEnd + e)
        {
            from = sunrise; to = daySky;
            k = Mathf.InverseLerp(sunriseEnd - e, sunriseEnd + e, t);
        }
        // --- Day ---
        else if (t < dayEnd - e)
        {
            from = daySky; to = daySky; k = 0f;
        }
        else if (t < dayEnd + e)
        {
            from = daySky; to = sunset;
            k = Mathf.InverseLerp(dayEnd - e, dayEnd + e, t);
        }
        // --- Sunset ---
        else if (t < sunsetEnd - e)
        {
            from = sunset; to = sunset; k = 0f;
        }
        else
        {
            from = sunset; to = night;
            k = Mathf.InverseLerp(sunsetEnd - e, sunsetEnd, t);
        }

        // 블렌드 곡선
        k = Mathf.Clamp01(k);
        if (BLEND_SHARPNESS != 1f)
            k = Mathf.Pow(k, BLEND_SHARPNESS);

        skyMat.SetTexture("_Tex1", from);
        skyMat.SetTexture("_Tex2", to);
        skyMat.SetFloat("_Blend", k);

        // 회전
        float rot = skyMat.GetFloat("_Rotation");
        rot += ROTATION_SPEED * Time.deltaTime;
        if (rot > 360f) rot -= 360f;
        skyMat.SetFloat("_Rotation", rot);
    }
}