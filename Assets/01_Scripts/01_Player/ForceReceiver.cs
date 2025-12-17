using UnityEngine;

/// <summary>
/// 외력(넉백 등) + 중력/점프를 한 곳에서 관리
/// CharacterController.Move에 더할 이동량(Vector3)을 제공
/// </summary>
public class ForceReceiver : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float maxFallSpeed = -40f;

    [Header("External Force")]
    [SerializeField] private float drag = 6f;

    private Vector3 impact;          // 수평 외력 누적
    public float VerticalVelocity { get; private set; }

    public void AddForce(Vector3 force)
    {
        // 점프력은 PlayerController에서 VerticalVelocity로 줌
        // 여기서는 넉백 같은 외력을 주로 처리
        impact += force;
    }

    public void SetJumpVelocity(float jumpVelocity)
    {
        VerticalVelocity = jumpVelocity;
    }

    public Vector3 ConsumeMove(float deltaTime, bool isGrounded)
    {
        // 중력 처리
        if (isGrounded && VerticalVelocity < 0f)
            VerticalVelocity = groundedGravity;
        else
            VerticalVelocity += gravity * deltaTime;

        VerticalVelocity = Mathf.Max(VerticalVelocity, maxFallSpeed);

        // 외력 감쇠
        impact = Vector3.Lerp(impact, Vector3.zero, drag * deltaTime);

        return impact * deltaTime + Vector3.up * (VerticalVelocity * deltaTime);
    }
}