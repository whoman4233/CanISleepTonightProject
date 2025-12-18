using UnityEngine;

//상세보기용 인터페이스
public interface IInspectable 
{
    GameObject GetInspectPrefab();   // 시각용 프리팹
    void OnInspectionStart();         // 필드 비활성화
    void OnInspectionEnd();           // 필드 복구
}

//상호작용 인터페이스

public interface IInteractable
{
    bool CanInteract { get; }
}

//상세보기 프리펩 클릭 가능한 검사 대상
public interface IInspectTarget
{
    void OnInspect(IInspectable owner);
}
