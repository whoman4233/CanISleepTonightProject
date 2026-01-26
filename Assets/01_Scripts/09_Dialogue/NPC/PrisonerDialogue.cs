using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerDialogue : MonoBehaviour, IInteractable
{
    public VisualAnomalyType myVisualType;
    //private string _mySpeakerKey = null; // 기본값은 null
    private DialogueManager dialogueManager;

    private static readonly Dictionary<VisualAnomalyType, string> SpeakerMapping = new()
{
    { VisualAnomalyType.BikiniModel, DialogueKeys.Speakers.Mimi } // 미미 = 비키니모델로 정의 (비주얼타입과 csv의 스피커 불일치 해결)
};

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = GameObject.FindAnyObjectByType<DialogueManager>();
        }
    }

    //public virtual void Interact(Player player)
    //{
    //    if (this == null || !gameObject.activeInHierarchy) return;
    //    if (dialogueManager == null) return;

    //    if (myVisualType == VisualAnomalyType.None)
    //    {
    //        Debug.Log($"{gameObject.name}: 일반 죄수는 대사 데이터가 없습니다.");
    //        return;
    //    }

    //    // 매핑 테이블에서 CSV 스피커 키를 가져오고 정의되지 않았으면 enum이름을 그대로 사용
    //    if (!SpeakerMapping.TryGetValue(myVisualType, out string speakerKey))
    //    {
    //        speakerKey = myVisualType.ToString();
    //    }

    //    Debug.Log($"[Dialogue] 시도하는 스피커 키: {speakerKey}");
    //    dialogueManager.StartDialogueByKeys(speakerKey, DialogueKeys.Types.Dialogue);
    //}

    public virtual void Interact(Player player)
    {
        if (this == null || !gameObject.activeInHierarchy) return;

        // 기본적으로는 아무 콜백 없이 실행
        HandleDialogue(null);
    }

    protected void HandleDialogue(System.Action onComplete = null)
    {
        if (dialogueManager == null) return;

        if (!SpeakerMapping.TryGetValue(myVisualType, out string speakerKey))
            speakerKey = myVisualType.ToString();

        dialogueManager.StartDialogueByKeys(speakerKey, DialogueKeys.Types.Dialogue, onComplete);
    }
}

