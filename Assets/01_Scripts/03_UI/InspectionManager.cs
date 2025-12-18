using System;
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

    private Action<InspectionViewReadyEvent> _onViewReady;

    private RectTransform inspectionViewRect;

    private PlayerInputs _inputs;

    private bool isInspecting;
    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    private void Awake()
    {
        _inputs = GetComponentInParent<Player>().Inputs;
        inspectionCamera.gameObject.SetActive(false);
        _onViewReady = OnViewReady;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewReady);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onViewReady);
    }

    private void Update()
    {
        if (!isInspecting)
            return;

        if (_inputs.Inspection.Exit.WasPressedThisFrame())
        {
            ExitInspection();
            return;
        }

        HandleRotation();
        HandleInspectClick();
    }

    // =========================
    // Inspection Lifecycle
    // =========================

    public void EnterInspection(IInspectable inspectable)
    {
        if (inspectable == null)
            return;

        isInspecting = true;
        currentInspectable = inspectable;

        _inputs.Player.Disable();
        _inputs.Inspection.Enable();

        inspectionCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetRotation();
        SpawnInspectObject(inspectable.GetInspectPrefab());

        EventBus.Publish(new InspectionViewRequestedEvent());
    }

    public void ExitInspection()
    {
        isInspecting = false;

        if (inspectInstance != null)
        {
            Destroy(inspectInstance);
            inspectInstance = null;
        }

        _inputs.Inspection.Disable();
        _inputs.Player.Enable();

        currentInspectable?.OnInspectionEnd();
        currentInspectable = null;

        inspectionCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        inspectionViewRect = null;
        EventBus.Publish(new InspectionViewReleasedEvent());
    }

    // =========================
    // View Binding
    // =========================

    private void OnViewReady(InspectionViewReadyEvent e)
    {
        inspectionViewRect = e.ViewRect;
    }

    // =========================
    // Rotation
    // =========================

    private void HandleRotation()
    {
        if (!_inputs.Inspection.RotateHold.IsPressed())
            return;

        Vector2 delta = _inputs.Inspection.Rotate.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 0.001f)
            return;

        yaw += delta.x * rotateSpeed;
        pitch -= delta.y * rotateSpeed;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        inspectPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ResetRotation()
    {
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;
    }

    // =========================
    // Inspect Click (Ray)
    // =========================

    private void HandleInspectClick()
    {
        Debug.Log("A: HandleInspectClick 진입");

        if (inspectionViewRect == null)
        {
            Debug.Log(" inspectionViewRect == null");
            return;
        }

        if (!_inputs.Inspection.InspectClick.WasPressedThisFrame())
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                inspectionViewRect,
                screenPos,
                null))
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inspectionViewRect,
            screenPos,
            null,
            out Vector2 localPoint);

        Rect rect = inspectionViewRect.rect;

        float u = (localPoint.x - rect.xMin) / rect.width;
        float v = (localPoint.y - rect.yMin) / rect.height;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;

        Ray ray = inspectionCamera.ViewportPointToRay(
            new Vector3(u, v, 0f)
        );

        Debug.DrawRay(ray.origin, ray.direction * inspectRayDistance, Color.green, 1.5f);

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

        if (inspectInstance.TryGetComponent<IInspectionView>(out var view))
        {
            view.Bind(currentInspectable);
        }
    }
}






