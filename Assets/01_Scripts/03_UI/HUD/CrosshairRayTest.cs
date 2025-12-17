using UnityEngine;

public class CrosshairRayTest : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Reference")]
    [SerializeField] private CrosshairController crosshair;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        bool canInteract = false;

        if (cam != null &&
            Physics.Raycast(
                cam.transform.position,
                cam.transform.forward,
                out RaycastHit hit,
                rayDistance,
                interactLayer))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                canInteract = interactable.CanInteract;
            }
        }

        crosshair.SetInteractable(canInteract);
    }
}
