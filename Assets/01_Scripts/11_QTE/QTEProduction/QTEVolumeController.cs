using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class QTEVolumeController : MonoBehaviour
{
    [Header("Volume Binding")]
    [Tooltip("씬에서 QTEGlobalVolumeTag를 찾아 자동 바인딩")]
    [SerializeField] private Volume qteVolume;

    [Header("Blend (Weight)")]
    [Min(0f)][SerializeField] private float blendInTime = 0.15f;
    [Min(0f)][SerializeField] private float blendOutTime = 0.15f;

    [Header("Auto Focus (Optional)")]
    [SerializeField] private bool autoFocusToTarget = true;
    [Tooltip("타겟 거리 계산에 더해지는 오프셋(미세 조정).")]
    [SerializeField] private float focusOffset = 0.0f;
    [Min(0f)][SerializeField] private float focusLerpSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool logBinding = false;

    private DepthOfField _dof;
    private Coroutine _blendCo;
    private Transform _focusTarget;
    private Camera _mainCam;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 활성화 시점에도 한 번 시도 (DontDestroy / 씬 전환 대응)
        TryBind();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ForceReset();
    }

    private void OnDestroy()
    {
        ForceReset();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 바인딩 재시도 (씬마다 다른 볼륨을 쓸 수 있음)
        TryBind();
    }

    private void Update()
    {
        if (!autoFocusToTarget || _dof == null || _focusTarget == null)
            return;

        if (_mainCam == null)
            _mainCam = Camera.main;

        if (_mainCam == null)
            return;

        float dist = Vector3.Distance(_mainCam.transform.position, _focusTarget.position) + focusOffset;
        _dof.focusDistance.value = Mathf.Lerp(
            _dof.focusDistance.value,
            dist,
            Time.deltaTime * focusLerpSpeed
        );
    }

    // =========================
    // Public API
    // =========================

    public void Enter(Transform focusTarget)
    {
        _focusTarget = focusTarget;

        if (!TryBind())
            return;

        StartBlend(1f, blendInTime);
    }

    public void Exit()
    {
        _focusTarget = null;

        if (qteVolume == null)
            return;

        StartBlend(0f, blendOutTime);
    }

    // =========================
    // Internals
    // =========================

    private bool TryBind()
    {
        // 1) 직접 할당되어 있으면 그대로 사용
        if (qteVolume != null)
            return CacheOverrides();

        // 2) 씬에서 태그로 찾기
        var tag = FindObjectOfType<QTEGlobalVolumeTag>(true);
        if (tag != null)
            qteVolume = tag.Volume;

        if (qteVolume == null)
        {
            if (logBinding)
                Debug.Log("[QTEVolumeController] QTE volume not found in this scene.");
            return false;
        }

        // 바인딩 직후 안전 초기화: QTE가 아닌 상태에서 잔류 방지
        qteVolume.weight = 0f;

        if (logBinding)
            Debug.Log($"[QTEVolumeController] Bound volume: {qteVolume.name}");

        return CacheOverrides();
    }

    private bool CacheOverrides()
    {
        if (qteVolume.profile == null)
        {
            Debug.LogWarning("[QTEVolumeController] Volume has no profile.");
            return false;
        }

        if (_dof == null)
            qteVolume.profile.TryGet(out _dof);

        return true;
    }

    private void StartBlend(float targetWeight, float time)
    {
        if (_blendCo != null)
        {
            StopCoroutine(_blendCo);
            _blendCo = null;
        }

        _blendCo = StartCoroutine(BlendTo(targetWeight, time));
    }

    private IEnumerator BlendTo(float target, float time)
    {
        if (qteVolume == null)
            yield break;

        float start = qteVolume.weight;

        if (time <= 0f)
        {
            qteVolume.weight = target;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            qteVolume.weight = Mathf.Lerp(start, target, t);
            yield return null;
        }

        qteVolume.weight = target;
        _blendCo = null;
    }

    private void ForceReset()
    {
        _focusTarget = null;
        _mainCam = null;

        if (_blendCo != null)
        {
            StopCoroutine(_blendCo);
            _blendCo = null;
        }

        // 잔류 방지
        if (qteVolume != null)
            qteVolume.weight = 0f;
    }
}