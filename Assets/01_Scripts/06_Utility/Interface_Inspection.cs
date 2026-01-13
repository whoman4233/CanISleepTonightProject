using UnityEngine;

//상세보기용 인터페이스
public interface IInspectable 
{
    GameObject GetInspectPrefab();   // 시각용 프리팹
    Transform GetInspectPivot();
    void OnInspectionEnter();
    void OnInspectionExit();
}

//상세보기 프리펩 클릭 가능한 검사 대상
public interface IInspectTarget
{
    void OnInspect(IInspectable inspectable);
}

//상세보기 상호작용 후 월드 오브젝트 적용
public interface IInspectionView
{
    void Bind(IInspectable inspectable);
}

//상세보기 시 InspectTarget 슬롯ID 전달용
public interface IHiddenItemInteractable
{
    void TryRevealItem(HiddenItemStateSO itemDefinition);
}

//휴지처럼 가린 것 상세보기에서 제거용
public interface IInspectAction
{
    void InspectAction(IInspectable owner);
}
