using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{
    private const float ViewportCenterX = 0.5f;
    private const float ViewportCenterY = 0.5f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Ray Settings")]
    [SerializeField] private float interactDistance = 1f;
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;

    private Player _player;

    // UI/다른 시스템이 읽을 수 있는 "현재 타겟" 정보
    public bool HasTarget => _currentInteractable != null;
    public GameObject CurrentTargetObject => _currentHitCollider ? _currentHitCollider.gameObject : null;
    public float CurrentTargetDistance => _currentHitDistance;

    private IInteractable _currentInteractable;
    private Collider _currentHitCollider;
    private float _currentHitDistance;

    private void Awake()
    {
        _player = GetComponent<Player>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (_player == null)
        {
            Debug.LogError("[PlayerInteractor] Player 컴포넌트를 찾지 못했습니다.");
            enabled = false;
        }

        if (targetCamera == null)
        {
            Debug.LogError("[PlayerInteractor] Camera가 비어있습니다. Inspector에 할당하거나 MainCamera 태그를 확인하세요.");
            enabled = false;
        }
    }

    private void Update()
    {
        // 상시 스캔(감지)
        Scan();
    }

    /// <summary>
    /// 매 프레임: 화면 중앙으로 Raycast해서 현재 Interactable을 캐싱
    /// </summary>
    private void Scan()
    {
        _currentInteractable = null;
        _currentHitCollider = null;
        _currentHitDistance = 0f;

        Ray ray = targetCamera.ViewportPointToRay(new Vector3(ViewportCenterX, ViewportCenterY, 0f));

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green, 0f);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Ignore))
            return;

        _currentHitCollider = hit.collider;
        _currentHitDistance = hit.distance;

        if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
        {
            _currentInteractable = interactable;
            return;
        }

        _currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
    }

    /// <summary>
    /// E키 눌렀을 때만 호출: 현재 캐싱된 대상이 있으면 상호작용 실행
    /// </summary>
    public bool TryInteract()
    {
        if (_currentInteractable == null)
            return false;

        _currentInteractable.Interact(_player);
        return true;
    }
}