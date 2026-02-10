using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOutLiner : MonoBehaviour
{
    [System.Serializable]
    public struct HighlightStep
    {
        public DialogueKeys.DialogueType step; // 어떤 단계에서
        public InteractableOutliner target;    // 어떤 오브젝트를 켤 것인가
    }

    [Header("Highlight Settings")]
    [SerializeField] private List<HighlightStep> highlightSteps;
    [SerializeField] private Color tutorialColor = Color.blue; // 아웃라인 색상
    [SerializeField] private float tutorialWidth = 0.05f; // 아웃라인 두께

    private static readonly int OutlineWidthId = Shader.PropertyToID("_Scale"); // 쉐이더 프로퍼티 ID

    private InteractableOutliner _currentActiveOutliner;
    private Coroutine _blinkCoroutine;

    private void OnEnable()
    {
        EventBus.Subscribe<DialogueStepChangedEvent>(OnStepChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DialogueStepChangedEvent>(OnStepChanged);
    }

    private void OnStepChanged(DialogueStepChangedEvent e)
    {
        // 1. 기존 강조 종료 및 두께 초기화
        if (_currentActiveOutliner != null)
        {
            if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);

            _currentActiveOutliner.SetHighlight(false);
            SetWidth(_currentActiveOutliner, 0f); // 두께 초기화
            _currentActiveOutliner.ResetColorToDefault();
            _currentActiveOutliner = null;
        }

        // 2. 새로운 타겟 탐색
        HighlightStep targetStep = highlightSteps.Find(x => x.step == e.NewStep);

        if (targetStep.target != null)
        {
            _currentActiveOutliner = targetStep.target;

            // 3. 하이라이트 적용 (색상)
            _currentActiveOutliner.SetHighlight(true, tutorialColor);

            // 4. [핵심] 두께 적용 (깜빡이는 연출 추가하면 더 눈에 띔)
            _blinkCoroutine = StartCoroutine(BlinkWidthRoutine(_currentActiveOutliner));

            Debug.Log($"[TutorialHighlight] {e.NewStep} 단계 강조: {targetStep.target.name} (Width: {tutorialWidth})");
        }
    }
    private IEnumerator BlinkWidthRoutine(InteractableOutliner outliner)
    {
        while (true)
        {
            // 두께를 주기적으로 변화시킴 (예: 설정값의 70% ~ 130% 사이)
            float animatedWidth = tutorialWidth * (Mathf.Sin(Time.time * 5f) * 0.3f);
            SetWidth(outliner, animatedWidth);
            yield return null;
        }
    }
    private void SetWidth(InteractableOutliner outliner, float width)
    {
        if (outliner == null) return;

        // Reflection이나 내부 구조 접근 대신 직접 MPB를 쏴줍니다.
        // InteractableOutliner가 사용하는 렌더러 목록을 가져오기 위해 GetComponents 사용
        Renderer[] renderers = outliner.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetFloat(OutlineWidthId, width);
            r.SetPropertyBlock(mpb);
        }
    }
}
