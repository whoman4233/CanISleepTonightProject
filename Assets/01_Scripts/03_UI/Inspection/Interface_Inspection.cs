using UnityEngine;

public interface IInspectable //상세보기 상호작용 전용 인터페이스
{
    GameObject GetInspectSource();   // Inspection에 사용할 원본
    void OnInspectionStart();        // 필드 비활성화 등
    void OnInspectionEnd();          // 필드 복구
}
