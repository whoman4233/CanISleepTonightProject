using UnityEngine;

public class InspectTarget : MonoBehaviour, IInspectTarget
{
    [SerializeField] private MonoBehaviour actionBehaviour;
    [SerializeField] private bool revealOnInspect; // 컨테이너는 true, 무기/금지물품은 false
    private IInspectAction _action;
    private bool _isRevealed;

    public bool CanShowOutline => _isRevealed;

    public void MarkRevealed()
    {
        _isRevealed = true;
    }
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
