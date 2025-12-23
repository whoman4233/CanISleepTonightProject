using UnityEngine;

public sealed class RagdollSetting : MonoBehaviour
{
    [Header("References (Inspector)")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody mainRigidbody;
    [SerializeField] private Collider mainCollider;

    [Header("Disable On Ragdoll (Optional)")]
    [SerializeField] private Behaviour[] disableBehaviours;

    // =========================
    // Settings (Script Only)
    // =========================
    private const bool UseDebugForce = false;
    private const float DebugForce = 300f;

    private const float ImpactForceMultiplier = 1f;
    private const float MinImpactForce = 3f;

    private const float UpwardBiasAmount = 0.5f;
    private const float ExtraUpwardBoost = 1.0f;

    private const float DirectionEpsilonSqr = 0.0001f;

    // 과장 연출 (날아감/휘청)
    private const ForceMode ImpactForceMode = ForceMode.VelocityChange;
    private const float LaunchMultiplier = 1.0f;
    private const float TorqueMultiplier = 0.6f;

    // 분산 타격
    private const int SpreadBodyCount = 3;
    private const float SpreadRadius = 0.8f; // 히트 지점 주변 몇 m까지 분산할지

    // 물리 안정화
    private const CollisionDetectionMode RagdollCollisionMode = CollisionDetectionMode.ContinuousDynamic;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdoll;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (mainRigidbody == null) mainRigidbody = GetComponent<Rigidbody>();
        if (mainCollider == null) mainCollider = GetComponent<Collider>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(includeInactive: true);
        ragdollColliders = GetComponentsInChildren<Collider>(includeInactive: true);

        SetRagdollActive(false);
    }

    public void ApplyImpact(Vector3 hitPoint, Vector3 direction, float strength)
    {

        if (!isRagdoll)
            SetRagdollActive(true);

        float forceMagnitude = UseDebugForce
            ? DebugForce
            : Mathf.Max(strength * ImpactForceMultiplier, MinImpactForce);

        // 날아감 과장
        forceMagnitude *= LaunchMultiplier;

        Vector3 forceDirection = (direction.sqrMagnitude > DirectionEpsilonSqr)
            ? direction.normalized
            : -transform.forward;

        forceDirection = (forceDirection + (Vector3.up * (UpwardBiasAmount + ExtraUpwardBoost))).normalized;

        ApplySpreadForce(hitPoint, forceDirection, forceMagnitude);
        ApplyTorque(hitPoint, forceDirection, forceMagnitude);
    }
    private void SetRagdollActive(bool enable)
    {
        isRagdoll = enable;

        if (animator != null)
            animator.enabled = !enable;

        // 루트 이동용 콜라이더는 레그돌에서 꺼서 "중복 충돌" 방지
        if (mainCollider != null)
            mainCollider.enabled = !enable;

        // 루트 Rigidbody는 프로젝트 이동방식에 따라 다르지만,
        // 레그돌에서는 보통 "움직임 제어에서 분리"하기 위해 kinematic 유지
        if (mainRigidbody != null)
            mainRigidbody.isKinematic = true;

        // 레그돌 뼈대 물리 토글
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            Rigidbody rb = ragdollRigidbodies[i];
            if (rb == null || rb == mainRigidbody) continue;

            rb.isKinematic = !enable;

            if (enable)
            {
                rb.detectCollisions = true;
                rb.collisionDetectionMode = RagdollCollisionMode;
                rb.WakeUp(); // ★ 레그돌 후 "물리가 안 움직임" 방지 핵심
            }
        }

        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            Collider col = ragdollColliders[i];
            if (col == null || col == mainCollider) continue;

            col.enabled = enable;
        }

        // 이동 AI / Agent / Controller 끄기(있으면)
        if (enable && disableBehaviours != null)
        {
            for (int i = 0; i < disableBehaviours.Length; i++)
                if (disableBehaviours[i] != null) disableBehaviours[i].enabled = false;
        }
    }

    private Rigidbody FindClosestBody(Vector3 hitPoint)
    {
        Rigidbody closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            Rigidbody rb = ragdollRigidbodies[i];
            if (rb == null || rb == mainRigidbody) continue;

            float dist = Vector3.Distance(rb.worldCenterOfMass, hitPoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = rb;
            }
        }
        return closest;
    }

    private void ApplySpreadForce(Vector3 hitPoint, Vector3 dir, float magnitude)
    {
        int applied = 0;

        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            Rigidbody rb = ragdollRigidbodies[i];
            if (rb == null || rb == mainRigidbody) continue;

            float dist = Vector3.Distance(rb.worldCenterOfMass, hitPoint);
            if (dist > SpreadRadius) continue;

            rb.WakeUp();
            rb.AddForce(dir * magnitude, ImpactForceMode);

            applied++;
            if (applied >= SpreadBodyCount)
                break;
        }

        if (applied == 0)
        {
            Rigidbody closest = FindClosestBody(hitPoint);
            if (closest != null)
            {
                closest.WakeUp();
                closest.AddForce(dir * magnitude, ImpactForceMode);
            }
        }
    }

    private void ApplyTorque(Vector3 hitPoint, Vector3 dir, float magnitude)
    {
        Rigidbody target = FindClosestBody(hitPoint);
        if (target == null) return;

        Vector3 torqueAxis = Vector3.Cross(Vector3.up, dir).normalized;
        float torqueAmount = magnitude * TorqueMultiplier;

        target.WakeUp();
        target.AddTorque(torqueAxis * torqueAmount, ImpactForceMode);
    }
}