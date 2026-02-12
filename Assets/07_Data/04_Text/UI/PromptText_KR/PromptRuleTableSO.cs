using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Prompt Rule Table")]
public class PromptRuleTableSO : ScriptableObject
{
    public List<PromptRule> rules;

    public bool TryGetPromptId(
        string objectType,
        string state,
        PromptContext context,
        out string promptId)
    {
        foreach (var r in rules)
        {
            if (r.objectType == objectType &&
                r.state == state &&
                r.context == context)
            {
                promptId = r.promptId;
                return true;
            }
        }

        promptId = null;
        return false;
    }
}

[Serializable]
public class PromptRule
{
    public string objectType;
    public string state;
    public PromptContext context;
    public string promptId;
}
