using UnityEngine;
using UnityEngine.InputSystem;

public class InspectionManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Transform inspectPivot;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 0.15f;
    [SerializeField] private float pitchLimit = 80f;

    [Header("Ray")]
    [SerializeField] private LayerMask inspectLayerMask;
    [SerializeField] private float inspectRayDistance = 5f;

    // =========================
    // Input
    // =========================
    private PlayerInputs inspectionInputs;

    // =========================
    // State
    // =========================
    private bool isInspecting;
    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    // =========================
    // [TEST ONLY]
    // =========================
    [SerializeField] private MonoBehaviour testInspectableMono;
    private IInspectable testInspectable;

    private void Awake()
    {
        // TEST Inspectable
        testInspectable = testInspectableMono as IInspectable;

        inspectionInputs = new PlayerInputs();
        inspectionInputs.Inspection.Enable();

        inspectionCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        // =========================
        // TEST ONLY : 강제 진입
        // =========================
        if (!isInspecting && Keyboard.current.iKey.wasPressedThisFrame)
        {
            EnterInspection(testInspectable);
        }

        // =========================
        // TEST ONLY : 강제 종료
        // =========================
        if (isInspecting && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitInspection();
        }

        if (!isInspecting)
            return;

        HandleRotation();
        HandleInspectClick();
    }

    private void OnDestroy()
    {
        inspectionInputs?.Dispose();
    }

    // =========================
    // Public API (유지 대상)
    // =========================

    public void EnterInspection(IInspectable inspectable)
    {
        if (inspectable == null)
            return;

        isInspecting = true;
        currentInspectable = inspectable;

        currentInspectable.OnInspectionStart();

        EventBus.Publish(new InspectionStartedEvent
        {
            Target = inspectable
        });

        inspectionCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetRotation();
        SpawnInspectObject(inspectable.GetInspectPrefab());
    }

    public void ExitInspection()
    {
        // ❗ 여러 번 불려도 안전해야 함
        isInspecting = false;

        if (inspectInstance != null)
        {
            Destroy(inspectInstance);
            inspectInstance = null;
        }

        currentInspectable?.OnInspectionEnd();
        currentInspectable = null;

        EventBus.Publish(new InspectionEndedEvent());

        inspectionCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =========================
    // Rotation
    // =========================

    private void HandleRotation()
    {
        if (!inspectionInputs.Inspection.RotateHold.IsPressed())
            return;

        Vector2 delta = inspectionInputs.Inspection.Rotate.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 0.001f)
            return;

        yaw += delta.x * rotateSpeed;
        pitch -= delta.y * rotateSpeed;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        inspectPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void ResetRotation()
    {
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;
    }

    // =========================
    // Inspect Click
    // =========================

    private void HandleInspectClick()
    {
        if (!inspectionInputs.Inspection.InspectClick.WasPressedThisFrame())
            return;

        Ray ray = inspectionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, inspectRayDistance, inspectLayerMask))
        {
            if (hit.collider.TryGetComponent<IInspectTarget>(out var target))
            {
                target.OnInspect(currentInspectable);
            }
        }
    }

    // =========================
    // Spawn
    // =========================

    private void SpawnInspectObject(GameObject prefab)
    {
        if (prefab == null)
            return;

        inspectInstance = Instantiate(prefab, inspectPivot);
        inspectInstance.transform.localPosition = Vector3.zero;
        inspectInstance.transform.localRotation = Quaternion.identity;
        inspectInstance.transform.localScale = Vector3.one;
    }
}




