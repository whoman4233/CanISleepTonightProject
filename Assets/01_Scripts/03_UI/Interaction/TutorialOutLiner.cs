using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOutLiner : MonoBehaviour
{
    [System.Serializable]
    public struct HighlightStep
    {
        public DialogueKeys.DialogueType step;
        public GameObject target; // InteractableOutliner 대신 GameObject로 범용성 확보
    }

    [Header("Highlight Settings")]
    [SerializeField] private List<HighlightStep> highlightSteps;
    [SerializeField] private Color highlightColor = Color.yellow; // 노란색 강조
    [SerializeField][Range(0f, 5f)] private float intensity = 2f; // 발광 세기

    // 쉐이더 프로퍼티 ID (대부분의 Standard/URP 쉐이더 공용)
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 기준

    private GameObject _currentActiveTarget;
    private Coroutine _blinkCoroutine;

    private void OnEnable() => EventBus.Subscribe<DialogueStepChangedEvent>(OnStepChanged);
    private void OnDisable() => EventBus.Unsubscribe<DialogueStepChangedEvent>(OnStepChanged);

    private void OnStepChanged(DialogueStepChangedEvent e)
    {
        // 1. 기존 강조 종료
        if (_currentActiveTarget != null)
        {
            if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
            SetObjectHighlight(_currentActiveTarget, Color.black, false); // 꺼짐
            _currentActiveTarget = null;
        }

        // 2. 새로운 타겟 탐색
        HighlightStep targetStep = highlightSteps.Find(x => x.step == e.NewStep);

        if (targetStep.target != null)
        {
            _currentActiveTarget = targetStep.target;
            // 3. 깜빡이는 루틴 시작 (노란색 전체 발광)
            _blinkCoroutine = StartCoroutine(BlinkEmissionRoutine(_currentActiveTarget));
        }
    }

    private IEnumerator BlinkEmissionRoutine(GameObject target)
    {
        while (true)
        {
            // Sin 함수를 이용해 0.5 ~ intensity 사이를 부드럽게 왕복
            float pingPong = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float currentIntensity = Mathf.Lerp(0.5f, intensity, pingPong);

            Color finalColor = highlightColor * currentIntensity;
            SetObjectHighlight(target, finalColor, true);

            yield return null;
        }
    }

    private void SetObjectHighlight(GameObject target, Color color, bool enable)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            // Emission 컬러 적용 (HDR 효과)
            mpb.SetColor(EmissionColorId, color);
            r.SetPropertyBlock(mpb);

            // 실시간으로 키워드를 켜줘야 하는 경우 (Standard Shader 대응)
            if (enable) r.material.EnableKeyword("_EMISSION");
            else r.material.DisableKeyword("_EMISSION");
        }
    }
}
