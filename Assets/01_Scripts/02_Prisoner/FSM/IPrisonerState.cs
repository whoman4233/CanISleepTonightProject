using UnityEngine;

public interface IPrisonerState
{
    void Enter();            // 상태 진입 시
    void Update();           // 매 프레임 실행
    void Exit();             // 상태를 나갈 때
    void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir); // 피격 시 대응 전략
}