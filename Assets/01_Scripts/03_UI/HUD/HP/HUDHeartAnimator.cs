using UnityEngine;
using System.Collections;

public class HUDHeartAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform heartIcon;

    [Header("Beat Speed (Interval)")]
    [SerializeField] private float maxInterval = 0.8f;  // HP 100일 때 느린 박동
    [SerializeField] private float minInterval = 0.2f;  // HP 낮을 때 빠른 박동

    [Header("Width Pulse (Scale X)")]
    [Tooltip("수축 시 X 스케일 (1보다 작게). 예: 0.85면 폭이 85%로 줄어듦")]
    [SerializeField] private float minScaleX = 0.85f;

    [Tooltip("수축/복구에 걸리는 시간 비율 (interval 대비). 0.5면 반 수축, 반 복구")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float squeezeRatio = 0.5f;

    private Vector3 _originScale;
    private Coroutine _beatRoutine;

    private void Awake()
    {
        if (heartIcon == null)
            heartIcon = GetComponent<RectTransform>();

        _originScale = heartIcon.localScale;
    }

    private void OnEnable()
    {
        // HP 이벤트가 안 오더라도 "기본 박동"은 시작하도록
        StartBeat(1f);
    }

    private void OnDisable()
    {
        if (_beatRoutine != null)
        {
            StopCoroutine(_beatRoutine);
            _beatRoutine = null;
        }

        // 비활성화 시 원상복구
        if (heartIcon != null)
            heartIcon.localScale = _originScale;
    }

    public void UpdateByHp(float normalizedHp)
    {
        StartBeat(normalizedHp);
    }

    private void StartBeat(float normalizedHp)
    {
        float interval = Mathf.Lerp(minInterval, maxInterval, Mathf.Clamp01(normalizedHp));

        if (_beatRoutine != null)
            StopCoroutine(_beatRoutine);

        _beatRoutine = StartCoroutine(BeatRoutine(interval));
    }

    private IEnumerator BeatRoutine(float interval)
    {
        float squeezeTime = interval * squeezeRatio;
        float releaseTime = interval * (1f - squeezeRatio);

        while (true)
        {
            // 1) 수축(폭 줄이기)
            yield return LerpScaleX(_originScale.x, _originScale.x * minScaleX, squeezeTime);

            // 2) 복구(폭 되돌리기)
            yield return LerpScaleX(_originScale.x * minScaleX, _originScale.x, releaseTime);
        }
    }

    private IEnumerator LerpScaleX(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            var s = heartIcon.localScale;
            s.x = to;
            heartIcon.localScale = s;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // UI는 타임스케일 영향 안 받도록
            float a = Mathf.Clamp01(t / duration);

            var s = heartIcon.localScale;
            s.x = Mathf.Lerp(from, to, a);
            heartIcon.localScale = s;

            yield return null;
        }

        var end = heartIcon.localScale;
        end.x = to;
        heartIcon.localScale = end;
    }
}

