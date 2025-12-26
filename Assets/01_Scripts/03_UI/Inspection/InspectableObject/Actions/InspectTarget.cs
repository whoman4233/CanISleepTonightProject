using UnityEngine;

public class InspectTarget : MonoBehaviour, IInspectTarget
{
    [SerializeField] private MonoBehaviour actionBehaviour;
    private IInspectAction _action;

    private void Awake()
    {
        if (actionBehaviour == null)
        {
            Debug.LogWarning($"{name} InspectTarget에 ActionBehaviour가 비어 있음");
            return;
        }

        _action = actionBehaviour as IInspectAction;

        if (_action == null)
        {
            Debug.LogError(
                $"{name} ActionBehaviour 타입 오류: {actionBehaviour.GetType().Name} " +
                $"(IInspectAction 구현 필요)"
            );
        }
    }

    public void OnInspect(IInspectable inspectable)
    {
        if (_action == null)
            return;

        _action.InspectAction(inspectable);
    }
}
