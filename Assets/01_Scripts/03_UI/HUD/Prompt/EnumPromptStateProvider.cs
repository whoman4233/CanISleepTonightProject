using UnityEngine;

public class EnumPromptStateProvider : MonoBehaviour, IPromptStateProvider
{
    [SerializeField] private MonoBehaviour source;
    [SerializeField] private string methodName = "GetPromptStateEnum";

    public string GetPromptState()
    {
        if (source == null)
            return null;

        var method = source.GetType().GetMethod(methodName);
        if (method == null)
            return null;

        var value = method.Invoke(source, null);
        return value?.ToString();
    }
}
