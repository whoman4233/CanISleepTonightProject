using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectHiddenItemAction : MonoBehaviour, IInspectAction
{
    [SerializeField] private HiddenItemStateSO itemDefinition;

    [Header("Mission Info")]
    [Tooltip("미션 전략(Strategy)에서 설정한 targetItemTag와 똑같이 적으세요.")]
    public string itemTag;

    public void InspectAction(IInspectable owner)
    {
        // 1. 기존 로직: 아이템 획득/공개 처리 (UI 갱신 등)
        if (owner is IHiddenItemInteractable interactable)
        {
            interactable.TryRevealItem(itemDefinition);
        }

        // 2. 🔥 [추가] 심판(GameFlowController)에게 점수 신고
        // "심판님! 저 방금 [Weapon] 태그가 달린 아이템을 찾았습니다!"
        if (DailyMissionManager.Instance != null)
        {
            // 태그가 비어있지 않을 때만 알림
            if (!string.IsNullOrEmpty(itemTag))
            {
                DailyMissionManager.Instance.NotifyItemFound(itemTag);
                Debug.Log($"[Action] 아이템 발견 신고함: {itemTag}");
            }
        }
        else
        {
            Debug.LogWarning("GameFlowController(DailyMissionManager)가 씬에 없습니다!");
        }
    }
}
