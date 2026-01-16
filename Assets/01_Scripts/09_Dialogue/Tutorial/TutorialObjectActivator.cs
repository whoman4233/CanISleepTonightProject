using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialObjectActivator : MonoBehaviour
{
    [Serializable]
    public struct TutorialObjectPair
    {
        public DialogueKeys.DialogueType step; // 어떤 단계에서
        public GameObject targetObject;        // 무엇을 켤 것인가
    }

    [Header("스텝별 활성화 설정")]
    [SerializeField] private List<TutorialObjectPair> activationList;

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
        Debug.Log($"[디버깅] 현재 들어온 이벤트 스텝: {e.NewStep}");
        foreach (var pair in activationList)
        {
            // 이벤트로 들어온 새로운 스텝과 일치하는 오브젝트를 활성화
            if (pair.step == e.NewStep)
            {
                if (pair.targetObject != null)
                {
                    pair.targetObject.SetActive(true);
                    Debug.Log($"[Tutorial] {e.NewStep} 단계: {pair.targetObject.name} 활성화");
                }
            }
        }
    }
}
