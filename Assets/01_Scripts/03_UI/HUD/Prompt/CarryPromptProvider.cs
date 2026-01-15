using UnityEngine;

public class CarryPromptProvider : MonoBehaviour, IPromptProvider
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PromptRuleTableSO ruleTable;

    private void Awake()
    {
        if (interactor == null)
            interactor = GetComponent<PlayerInteractor>();
    }

    public bool TryGetPromptId(PromptContext context, out string promptId)
    {
        promptId = null;

        if (context != PromptContext.Interact)
            return false;

        if (interactor == null || !interactor.IsCarrying)
            return false;

        var carried = interactor.CurrentHeldItem as ICarryable;
        if (carried == null)
            return false;

        string objectType = carried.GetCarryPromptObjectType();
        string state = CarryPromptState.CanDrop.ToString();

        return ruleTable != null &&
               ruleTable.TryGetPromptId(
                   objectType,
                   state,
                   context,
                   out promptId);
    }
}
