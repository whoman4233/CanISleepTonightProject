using UnityEngine;

/// <summary>
/// 1인칭 카메라 클리핑 방지 컨트롤러
/// - 카메라 위치를 이동시키지 않음
/// - Near Clip Plane만 동적으로 조절
/// - QTE / Shake / POV 연출과 충돌 없음
/// </summary>
public class FirstPersonCameraClipController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera cam;

    [Header("Near Clip Settings")]
    [Tooltip("장애물에 가까울 때 사용할 Near Clip")]
    [SerializeField] private float minNearClip = 0.05f;

    [Tooltip("기본 Near Clip 값")]
    [SerializeField] private float maxNearClip = 0.3f;

    [Tooltip("전방 장애물 감지 거리")]
    [SerializeField] private float checkDistance = 0.3f;

    [Tooltip("카메라 클리핑을 막을 레이어")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Lerp Speed")]
    [SerializeField] private float enterSpeed = 12f;
    [SerializeField] private float exitSpeed = 6f;

    private void Awake()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();
    }

    private void LateUpdate()
    {
        if (cam == null)
            return;

        // =====================================================
        // 카메라 전방으로 Ray 발사
        // =====================================================
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        bool hit = Physics.Raycast(
            ray,
            out RaycastHit hitInfo,
            checkDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        // =====================================================
        // 장애물에 가까우면 Near Clip을 줄여서
        // "얼굴이 벽 안으로 들어간 느낌" 제거
        // =====================================================
        float targetNearClip = hit ? minNearClip : maxNearClip;

        float speed = hit ? enterSpeed : exitSpeed;

        cam.nearClipPlane = Mathf.Lerp(
            cam.nearClipPlane,
            targetNearClip,
            Time.deltaTime * speed
        );
    }
}
