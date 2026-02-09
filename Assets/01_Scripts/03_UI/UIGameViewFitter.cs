using UnityEngine;

public class UIGameViewFitter : MonoBehaviour
{
    private RectTransform rect;
    private const float targetAspect = 16f / 9f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float scale = screenAspect / targetAspect;

        if (scale < 1f)
        {
            // 상하 레터박스 → 세로가 줄어든 게임화면
            rect.sizeDelta = new Vector2(1920f, 1080f * scale);
        }
        else
        {
            // 좌우 레터박스 → 가로가 줄어든 게임화면
            rect.sizeDelta = new Vector2(1920f / scale, 1080f);
        }
    }
}
