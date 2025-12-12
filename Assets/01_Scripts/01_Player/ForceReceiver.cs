using UnityEngine;

public class ForceReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Drag Settings")]
    [SerializeField] private float drag = 0.3f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;      // 일반 중력
    [SerializeField] private float groundedGravity = -0.5f; // 땅에 붙잡아 두는 약한 중력
    [SerializeField] private float maxFallSpeed = -25f;     // 최대 낙하 속도

    private float verticalVelocity;
    private Vector3 dampingVelocity;
    private Vector3 impact;

    /// <summary>
    /// 최종 이동 벡터(위쪽 속도 + 외부 힘)
    /// </summary>
    public Vector3 Movement => impact + Vector3.up * verticalVelocity;

    private void Start()
    {
        // 인스펙터에 안 넣어도 자동으로 찾아오도록
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
    }

    private void Update()
    {
        HandleGravity();
        HandleImpact();
    }

    private void HandleGravity()
    {
        bool isGrounded = controller != null && controller.isGrounded;

        if (isGrounded)
        {
            // 지면에 닿아있다면 항상 작고 일정한 음수값으로 고정
            if (verticalVelocity < groundedGravity)
            {
                verticalVelocity = groundedGravity;
            }
            else if (verticalVelocity > groundedGravity)
            {
                // 점프 직후 바로 isGrounded가 들어오는 경우를 대비해서
                verticalVelocity = groundedGravity;
            }
        }
        else
        {
            // 공중일 때만 중력을 계속 누적
            verticalVelocity += gravity * Time.deltaTime;

            // 너무 빠르게 떨어지지 않도록 하드 클램프
            if (verticalVelocity < maxFallSpeed)
            {
                verticalVelocity = maxFallSpeed;
            }
        }
    }

    private void HandleImpact()
    {
        // 넉백 등 감쇠
        impact = Vector3.SmoothDamp(
            current: impact,
            target: Vector3.zero,
            currentVelocity: ref dampingVelocity,
            smoothTime: drag
        );
    }

    public void Reset()
    {
        verticalVelocity = 0f;
        impact = Vector3.zero;
    }

    public void AddForce(Vector3 force)
    {
        impact += force;
    }

    public void Jump(float jumpForce)
    {
        // 점프는 무조건 위 방향 속도로 세팅
        verticalVelocity = jumpForce;
    }
}