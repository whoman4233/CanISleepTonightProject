using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOutLiner : MonoBehaviour
{

    public static TutorialOutLiner Instance { get; private set; }

    private Action<DialogueStepChangedEvent> _onStepChanged;

    private void Awake()
    {
        // 씬에 하나만 존재하도록 보장
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        _onStepChanged = e => OnStepChanged(e);
    }
    private void Start()
    {

        if (DialogueManager.Instance != null)
        {
            UpdateHighlight(DialogueKeys.DialogueType.Dialogue);
        }
    }

    private void OnDestroy()
    {
        // 튜토리얼이 끝나서 오브젝트가 파괴될 때 인스턴스 초기화
        if (Instance == this) Instance = null;
    }

    [System.Serializable]
    public struct HighlightStep
    {
        public DialogueKeys.DialogueType step;
        public GameObject target; // InteractableOutliner 대신 GameObject로 범용성 확보
        public GameObject vfxObject;
        public bool useHighlight;
    }

    [Header("Highlight Settings")]
    [SerializeField] private List<HighlightStep> highlightSteps;
    [SerializeField] private Color highlightColor = Color.yellow; // 노란색 강조
    [SerializeField][Range(0f, 5f)] private float intensity = 2f; // 발광 세기

    // 쉐이더 프로퍼티 ID (대부분의 Standard/URP 쉐이더 공용)
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissiveColorId = Shader.PropertyToID("EmissiveColor"); // 3ds Max 셰이더 그래프용
    private static readonly int EmissionColorUpperId = Shader.PropertyToID("_EMISSION_COLOR");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 기준

    private GameObject _currentActiveTarget;
    private Coroutine _blinkCoroutine;
    private GameObject _currentActiveVFX;

    private void OnEnable() => EventBus.Subscribe(_onStepChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onStepChanged);

    private void OnStepChanged(DialogueStepChangedEvent e)
    {
        UpdateHighlight(e.NewStep);
    }

    public void UpdateHighlight(DialogueKeys.DialogueType newStep)
    {
        StopCurrentHighlight();

        HighlightStep targetStep = highlightSteps.Find(x => x.step == newStep);

        // 하이라이트 실행
        if (targetStep.target != null && targetStep.useHighlight)
        {
            _currentActiveTarget = targetStep.target;
            _blinkCoroutine = StartCoroutine(BlinkEmissionRoutine(_currentActiveTarget));
        }

        // VFX 실행
        if (targetStep.vfxObject != null)
        {
            _currentActiveVFX = targetStep.vfxObject;
            _currentActiveVFX.SetActive(true);
        }
    }

    private IEnumerator BlinkEmissionRoutine(GameObject target)
    {
        float targetAlphaRatio = 200f / 255f;

        while (true)
        {
            // Sin 함수를 이용해 0.5 ~ intensity 사이를 부드럽게 왕복
            float pingPong = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
            float currentIntensity = intensity * pingPong * targetAlphaRatio;

            Color finalColor = highlightColor * currentIntensity;
            SetObjectHighlight(target, finalColor, true);

            yield return null;
        }
    }

    private void SetObjectHighlight(GameObject target, Color color, bool enable)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0 && enable)
        {
            Debug.LogWarning($"{target.name}에 렌더러가 없거나 비활성화되어 하이라이트 불가");
        }
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            // Emission 컬러 적용 (HDR 효과)
            mpb.SetColor(EmissionColorId, color);
            mpb.SetColor(EmissiveColorId, color);
            mpb.SetColor(EmissionColorUpperId, color);
            r.SetPropertyBlock(mpb);

            // 실시간으로 키워드를 켜줘야 하는 경우 (Standard Shader 대응)
            if (enable)
            {
                // 키워드 활성화
                r.sharedMaterial.EnableKeyword("_EMISSION");
                r.sharedMaterial.EnableKeyword("_EMISSION_COLOR");
            }
            else
            {
                r.sharedMaterial.DisableKeyword("_EMISSION");
                r.sharedMaterial.DisableKeyword("_EMISSION_COLOR");
            }
        }
    }
    public void StopCurrentHighlight()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (_currentActiveTarget != null)
        {
            SetObjectHighlight(_currentActiveTarget, Color.black, false);
            _currentActiveTarget = null;
        }
        if (_currentActiveVFX != null)
        {
            _currentActiveVFX.SetActive(false); // VFX 끄기
            _currentActiveVFX = null;
        }
    }
}
