using UnityEngine;

public class PromptProvider : MonoBehaviour, IPromptProvider
{
    [SerializeField] private string defaultPromptId;
    [SerializeField] private PromptRuleTableSO ruleTable;
    [SerializeField] private string objectType;

    private IPromptStateProvider stateProvider;

    private void Awake()
    {
        stateProvider = GetComponent<IPromptStateProvider>();
    }

    public bool TryGetPromptId(PromptContext context, out string promptId)
    {
        promptId = null;

        // =========================
        // Inspection 전용
        // =========================
        if (context == PromptContext.Inspection)
        {
            if (!string.IsNullOrEmpty(defaultPromptId))
            {
                promptId = defaultPromptId;
                return true;
            }
            return false;
        }

        // =========================
        // Interact 전용
        // =========================
        if (context != PromptContext.Interact)
            return false;

        if (stateProvider != null && ruleTable != null)
        {
            string state = stateProvider.GetPromptState();
            if (!string.IsNullOrEmpty(state) &&
                ruleTable.TryGetPromptId(objectType, state, context, out promptId))
                return true;
        }

        if (!string.IsNullOrEmpty(defaultPromptId))
        {
            promptId = defaultPromptId;
            return true;
        }

        return false;
    }
}
