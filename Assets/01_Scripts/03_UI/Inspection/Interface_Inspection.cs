using UnityEngine;

public interface IInspectable
{
    GameObject GetInspectPrefab();   // 시각용 프리팹
    void OnInspectionStart();         // 필드 비활성화
    void OnInspectionEnd();           // 필드 복구
}
