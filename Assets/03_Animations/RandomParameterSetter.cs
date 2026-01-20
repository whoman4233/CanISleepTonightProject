using UnityEngine;

public class RandomParameterSetter : StateMachineBehaviour
{
    [Header("Settings")]
    [Tooltip("랜덤 값을 넣을 애니메이터 파라미터 이름 (예: VisualIdleVariant)")]
    public string parameterName = "VisualIdleVariant";

    [Tooltip("랜덤 범위 최소값 (포함)")]
    public int minRange = 0;

    [Tooltip("랜덤 범위 최대값 (제외) - 예: 2를 넣으면 0, 1 중 선택됨")]
    public int maxRange = 2;

    // 상태에 진입할 때 1회 실행됨
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 범위 내에서 랜덤 값 뽑기
        int randomVal = Random.Range(minRange, maxRange);

        // 애니메이터 파라미터에 적용
        animator.SetInteger(parameterName, randomVal);

        // (디버그용 로그 - 필요 없으면 주석 처리)
        // Debug.Log($"[Animator] {parameterName} 랜덤 설정: {randomVal}");
    }
}