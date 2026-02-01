using UnityEngine;
using UnityEngine.UI;

public class HUDHeartBackgroundAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private Image backgroundImage;

    // =========================
    // HP 기반 크기
    // =========================
    [Header("HP Size Range")]
    [SerializeField] private Vector2 minSizeByHp = new Vector2(80f, 80f);
    [SerializeField] private Vector2 maxSizeByHp = new Vector2(240f, 240f);
    [SerializeField] private float sizeLerpSpeed = 6f;

    // =========================
    // Alpha Pulse
    // =========================
    [Header("Alpha Pulse")]
    [Tooltip("알파 최소값 (0~255 기준)")]
    [SerializeField] private float minAlpha = 40f;

    [Tooltip("알파 최대값 (0~255 기준)")]
    [SerializeField] private float maxAlpha = 160f;

    [Tooltip("알파 변화 속도")]
    [SerializeField] private float alphaPulseSpeed = 1.2f;

    // =========================
    // Runtime
    // =========================
    private Vector2 _currentSize;
    private Vector2 _targetSize;

    private float _alphaTime;

    // =========================
    // Lifecycle
    // =========================
    private void Awake()
    {
        if (backgroundRect == null)
            backgroundRect = GetComponent<RectTransform>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        _currentSize = backgroundRect.sizeDelta;
        _targetSize = _currentSize;
    }

    // =========================
    // HP 갱신 진입점
    // =========================
    public void UpdateByHp(float normalizedHp)
    {
        normalizedHp = Mathf.Clamp01(normalizedHp);

        _targetSize = Vector2.Lerp(
            maxSizeByHp,
            minSizeByHp,
            normalizedHp
        );
    }

    // =========================
    // Update
    // =========================
    private void Update()
    {
        // -------------------------
        // 1. Size (HP 기반)
        // -------------------------
        _currentSize = Vector2.Lerp(
            _currentSize,
            _targetSize,
            Time.unscaledDeltaTime * sizeLerpSpeed
        );

        backgroundRect.sizeDelta = _currentSize;

        // -------------------------
        // 2. Alpha Pulse
        // -------------------------
        _alphaTime += Time.unscaledDeltaTime * alphaPulseSpeed;

        float alpha01 = Mathf.Lerp(
            minAlpha / 255f,
            maxAlpha / 255f,
            Mathf.PingPong(_alphaTime, 1f)
        );

        Color c = backgroundImage.color;
        c.a = alpha01;
        backgroundImage.color = c;
    }
}


