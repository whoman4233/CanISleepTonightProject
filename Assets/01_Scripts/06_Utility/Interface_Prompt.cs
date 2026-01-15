public enum PromptContext
{
    Interact,      // PlayerInteractor
    Inspection     // InspectionManager
}

public interface IPromptProvider // 모든 프롬프트의 공통 인터페이스
{
    bool TryGetPromptId(
        PromptContext context,
        out string promptId
    );
}
public interface IPromptStateProvider // 상태가 있는 오브젝트에 선택적으로 붙이는 인터페이스(ex: 문/캐비닛)
{
    /// <summary>
    /// 현재 상태를 나타내는 키
    /// 예: "open", "closed", "locked"
    /// 상태 기반 프롬프트가 없으면 null / empty
    /// </summary>
    string GetPromptState();
}

