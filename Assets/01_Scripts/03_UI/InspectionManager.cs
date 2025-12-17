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

    private PlayerInputs inspectionInputs;   // ★ PlayerInputs 사용
    private bool isInspecting;

    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    #region Unity Lifecycle

    private void Awake()
    {
        inspectionInputs = new PlayerInputs();

        // Inspection ActionMap만 사용
        inspectionInputs.Inspection.Enable();
        inspectionInputs.Inspection.Disable();

        inspectionInputs.Inspection.Exit.performed += _ => ExitInspection();

        inspectionCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isInspecting)
            return;

        HandleRotation();
        HandleInspectClick();
    }

    private void OnDestroy()
    {
        inspectionInputs?.Dispose();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 상호작용 시스템에서 호출
    /// </summary>
    public void EnterInspection(IInspectable inspectable)
    {
        if (isInspecting || inspectable == null)
            return;

        isInspecting = true;
        currentInspectable = inspectable;

        currentInspectable.OnInspectionStart();

        // Player 입력 차단 요청
        EventBus.Publish(new InspectionStartedEvent());

        inspectionInputs.Inspection.Enable();

        inspectionCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetRotation();
        SpawnInspectObject(inspectable.GetInspectPrefab());
    }

    #endregion

    #region Exit

    private void ExitInspection()
    {
        if (!isInspecting)
            return;

        isInspecting = false;

        if (inspectInstance != null)
        {
            Destroy(inspectInstance);
            inspectInstance = null;
        }

        currentInspectable?.OnInspectionEnd();
        currentInspectable = null;

        inspectionInputs.Inspection.Disable();

        // Player 입력 복구 요청
        EventBus.Publish(new InspectionEndedEvent());

        inspectionCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region Rotation

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

    public void ResetRotation()   // UI 버튼용
    {
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;
    }

    #endregion

    #region Inspect Click

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

    #endregion

    #region Spawn

    private void SpawnInspectObject(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[InspectionManager] Inspect prefab is null.");
            return;
        }

        inspectInstance = Instantiate(prefab, inspectPivot);
        inspectInstance.transform.localPosition = Vector3.zero;
        inspectInstance.transform.localRotation = Quaternion.identity;
        inspectInstance.transform.localScale = Vector3.one;
    }

    #endregion
}



