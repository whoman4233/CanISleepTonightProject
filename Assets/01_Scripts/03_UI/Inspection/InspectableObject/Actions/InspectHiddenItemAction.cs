using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectHiddenItemAction : MonoBehaviour, IInspectAction
{
    [SerializeField] private HiddenItemDefinitionSO itemDefinition;

    [Header("Mission Info")]
    [Tooltip("미션 전략(Strategy)에서 설정한 targetItemTag와 똑같이 적으세요.")]
    public string itemTag;

    public virtual void InspectAction(IInspectable owner)
    {
        Debug.Log("[InspectHiddenItemAction] called");

        // 1. 상세보기에서 즉시 숨김
        gameObject.SetActive(false);

        // 2. 월드 상태 변경 (InspectionManager 기준)
        var manager = FindObjectOfType<InspectionManager>();
        if (manager == null || manager.CurrentWorldInspectable == null)
        {
            Debug.LogError("[InspectHiddenItemAction] World Inspectable not found");
            return;
        }

        var holder = manager.CurrentWorldInspectable.GetHiddenItemHolder();
        if (holder == null)
        {
            Debug.LogError("[InspectHiddenItemAction] HiddenItemHolder not found on world object");
            return;
        }

        holder.TryRevealItem(itemDefinition);

        // 3. 🔥 [추가] 심판(GameFlowController)에게 점수 신고
        // "심판님! 저 방금 [Weapon] 태그가 달린 아이템을 찾았습니다!"
        if (DailyMissionManager.Instance != null)
        {
            // 미션에 의미 있는 경우만 신고(ex:칼/망치/담배 등 미션용 아이템)
            if (itemDefinition.AffectsMission)
            {
                DailyMissionManager.Instance
                    ?.NotifyItemFound(itemDefinition.MissionTag);

                Debug.Log(
                    $"[Action] 아이템 발견 신고함: {itemDefinition.MissionTag}"
                );
            }
        }
        else
        {
            Debug.LogWarning("GameFlowController(DailyMissionManager)가 씬에 없습니다!");
        }
    }
}
