using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("플레이어 루트 Transform (CharacterController가 붙어있는 오브젝트)")]
    [SerializeField] private Transform playerRoot;

    [Header("Settings")]
    [Tooltip("QTE 진입 시 죄수 쪽을 바라보는 회전 속도")]
    [SerializeField] private float rotateSpeed = 12f;

    private bool _rotating;
    private Quaternion _targetRotation;

    // =========================
    // QTE Entry / Exit
    // =========================

    /// <summary>
    /// QTE 진입 시 호출.
    /// 플레이어 몸을 죄수 쪽으로 회전시킨다.
    /// </summary>
    public void EnterQTEMode(Transform attacker)
    {
        if (playerRoot == null || attacker == null)
            return;

        // 이동 / Look 입력 차단
        DisablePlayerInput();

        // 죄수 방향 계산 (Y축만)
        Vector3 dir = attacker.position - playerRoot.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        _targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        _rotating = true;
    }

    /// <summary>
    /// QTE 종료 시 호출.
    /// 회전은 유지하고 입력만 복구한다.
    /// </summary>
    public void ExitQTEMode()
    {
        _rotating = false;

        EnablePlayerInput();
    }

    private void Update()
    {
        if (!_rotating || playerRoot == null)
            return;

        // 플레이어 몸을 부드럽게 회전
        playerRoot.rotation = Quaternion.Slerp(
            playerRoot.rotation,
            _targetRotation,
            Time.deltaTime * rotateSpeed
        );
    }

    // =========================
    // Input Control
    // =========================

    private void DisablePlayerInput()
    {
        if (InputManager.Instance == null)
            return;

        if (InputManager.Instance.Inputs != null)
        {
            InputManager.Instance.Inputs.Player.Disable();
        }
    }

    private void EnablePlayerInput()
    {
        if (InputManager.Instance == null)
            return;

        if (InputManager.Instance.Inputs != null)
        {
            InputManager.Instance.Inputs.Player.Enable();
        }
    }
}



