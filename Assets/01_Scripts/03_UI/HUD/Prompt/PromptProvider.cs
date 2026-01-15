using UnityEngine;

public class PromptProvider : MonoBehaviour, IPromptProvider
{
    [Header("기본 프롬프트")]
    [SerializeField] private string defaultPromptId;

    [Header("상태 기반 프롬프트")]
    [SerializeField] private PromptRuleTableSO ruleTable;
    [SerializeField] private string objectType; // 문,캐비닛

    private IPromptStateProvider stateProvider;

    private void Awake()
    {
        stateProvider = GetComponent<IPromptStateProvider>();
    }

    public bool TryGetPromptId(PromptContext context, out string promptId)
    {
        promptId = null;

        //  상태 Provider가 있는 경우
        if (stateProvider != null && ruleTable != null)
        {
            string state = stateProvider.GetPromptState();
            if (!string.IsNullOrEmpty(state) &&
                ruleTable.TryGetPromptId(objectType, state, context, out var statePrompt))
            {
                promptId = statePrompt;
                return true;
            }
        }

        //  상태 Provider가 없는 경우 → 기본 상태 (CanPickUp)
        if (stateProvider == null && ruleTable != null)
        {
            string defaultState = CarryPromptState.CanPickUp.ToString();

            if (ruleTable.TryGetPromptId(
                    objectType,
                    defaultState,
                    context,
                    out var defaultPrompt))
            {
                promptId = defaultPrompt;
                return true;
            }
        }

        if (!string.IsNullOrEmpty(defaultPromptId))
        {
            promptId = defaultPromptId;
            return true;
        }
        return false;
    }

}
