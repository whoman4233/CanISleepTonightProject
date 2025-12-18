using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{
    private const float ViewportCenterX = 0.5f;
    private const float ViewportCenterY = 0.5f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Ray Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;

    private Player _player;

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

    public bool TryInteract()
    {
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(ViewportCenterX, ViewportCenterY, 0f));

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green, 1.0f);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[PlayerInteractor] Raycast MISS");
            return false;
        }

        Debug.Log($"[PlayerInteractor] HIT: {hit.collider.name}");

        if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
        {
            Debug.Log("[PlayerInteractor] Interactable FOUND on collider");
            interactable.Interact(_player);
            return true;
        }

        interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log("[PlayerInteractor] Interactable FOUND in parent");
            interactable.Interact(_player);
            return true;
        }

        Debug.Log("[PlayerInteractor] HIT but no IInteractable found");
        return false;
    }
}