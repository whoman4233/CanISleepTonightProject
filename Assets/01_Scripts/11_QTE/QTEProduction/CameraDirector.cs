using UnityEngine;
using Cinemachine;

public class CameraDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerRoot;              // Yaw 기준
    [SerializeField] private CinemachineVirtualCamera vcam;     // Virtual Camera

    [Header("Rotate Speed")]
    [SerializeField] private float yawRotateSpeed = 12f;
    [SerializeField] private float pitchRotateSpeed = 8f;

    [Header("QTE Camera")]
    [SerializeField] private float qteCameraDistance = 2.0f;    // 앉기 대응용

    // =========================
    // Cinemachine Components
    // =========================
    private CinemachinePOV pov;
    private Cinemachine3rdPersonFollow follow;

    // =========================
    // Runtime State
    // =========================
    private bool qteActive;

    private Quaternion targetYaw;

    private float targetPitch;
    private float originalPitch;

    private float originalCameraDistance;

    // =========================
    // Lifecycle
    // =========================
    private void Awake()
    {
        if (vcam == null)
            return;

        pov = vcam.GetCinemachineComponent<CinemachinePOV>();
        follow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        if (follow != null)
            originalCameraDistance = follow.CameraDistance;
    }

    // =========================
    // QTE Entry
    // =========================
    public void EnterQTEMode(Transform attacker)
    {
        if (attacker == null || vcam == null)
            return;

        DisablePlayerInput();

        // =========================
        // 1. Yaw 
        // =========================
        Vector3 flatDir = attacker.position - playerRoot.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            targetYaw = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        }

        // =========================
        // 2. Pitch (Cinemachine POV)
        // =========================
        Transform lookTarget = ResolveAttackerLookTarget(attacker);

        Vector3 camPos = vcam.transform.position;
        Vector3 dirToTarget = lookTarget.position - camPos;

        Quaternion lookRot = Quaternion.LookRotation(dirToTarget.normalized);
        targetPitch = NormalizePitch(lookRot.eulerAngles.x);

        if (pov != null)
        {
            originalPitch = pov.m_VerticalAxis.Value;
        }

        // =========================
        // 3. 앉기 대응: Camera Distance 고정
        // =========================
        if (follow != null)
        {
            originalCameraDistance = follow.CameraDistance;
            follow.CameraDistance = qteCameraDistance;
        }

        qteActive = true;
    }

    // =========================
    // QTE Exit
    // =========================
    public void ExitQTEMode()
    {
        if (!qteActive)
            return;

        qteActive = false;

        // Pitch 복구
        if (pov != null)
        {
            pov.m_VerticalAxis.Value = originalPitch;
        }

        // Camera Distance 복구
        if (follow != null)
        {
            follow.CameraDistance = originalCameraDistance;
        }

        EnablePlayerInput();
    }

    // =========================
    // Update
    // =========================
    private void Update()
    {
        if (!qteActive)
            return;

        // =========================
        // Yaw (플레이어 루트 회전)
        // =========================
        playerRoot.rotation = Quaternion.Slerp(
            playerRoot.rotation,
            targetYaw,
            Time.deltaTime * yawRotateSpeed
        );

        // =========================
        // Pitch (Cinemachine POV)
        // =========================
        if (pov != null)
        {
            pov.m_VerticalAxis.Value = Mathf.Lerp(
                pov.m_VerticalAxis.Value,
                targetPitch,
                Time.deltaTime * pitchRotateSpeed
            );
        }
    }

    // =========================
    // Helpers
    // =========================
    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private Transform ResolveAttackerLookTarget(Transform attacker)
    {
        var provider =
            attacker.GetComponentInChildren<IQTELookTargetProvider>(true);

        if (provider != null)
            return provider.GetQTELookTarget();

        return attacker;
    }

    private void DisablePlayerInput()
    {
        if (InputManager.Instance?.Inputs != null)
            InputManager.Instance.Inputs.Player.Disable();
    }

    private void EnablePlayerInput()
    {
        if (InputManager.Instance?.Inputs != null)
            InputManager.Instance.Inputs.Player.Enable();
    }
}







