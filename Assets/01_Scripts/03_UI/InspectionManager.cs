using UnityEngine;
using UnityEngine.InputSystem;

public class InspectionManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera inspectionCamera;

    [Header("Inspect Space")]
    [SerializeField] private Transform inspectPivot;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 0.15f;
    [SerializeField] private float pitchLimit = 80f;

    private bool isInspecting;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    private float yaw;
    private float pitch;

    // Input
    private PlayerInputs playerInputs;
    private PlayerInputs.PlayerActions playerActions;
    private PlayerInputs.InspectionActions inspectionActions;

    private InputAction rotate;
    private InputAction rotateHold;
    private InputAction exit;
    private InputAction reset;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeInput();
    }

    private void Update()
    {
        if (!isInspecting)
            return;

        HandleRotationInput();
    }

    #endregion

    #region Initialization

    private void InitializeInput()
    {
        PlayerController player = GetComponentInParent<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[InspectionManager] PlayerController not found.");
            return;
        }

        playerInputs = player.playerInput;
        playerActions = playerInputs.Player;
        inspectionActions = playerInputs.Inspection;

        rotate = inspectionActions.Rotate;
        rotateHold = inspectionActions.RotateHold;
        exit = inspectionActions.Exit;
        reset = inspectionActions.Reset;

        exit.performed += _ => ExitInspection();
        reset.performed += _ => ResetRotation();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Ray 기반 상호작용 시스템에서 호출
    /// </summary>
    public void EnterInspection(IInspectable inspectable)
    {
        if (isInspecting || inspectable == null)
            return;

        isInspecting = true;
        currentInspectable = inspectable;

        currentInspectable.OnInspectionStart();

        // 입력 전환
        playerActions.Disable();
        inspectionActions.Enable();

        // 카메라 활성
        inspectionCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetRotationInternal();
        SpawnInspectObject(inspectable.GetInspectPrefab());

        // =========================
        // UI / 연출 알림
        // =========================
        EventBus.Publish(new InspectionStartedEvent
        {
            Target = inspectable
        });
    }

    #endregion

    #region Inspection Flow

    private void ExitInspection()
    {
        if (!isInspecting)
            return;

        isInspecting = false;

        if (inspectInstance != null)
            Destroy(inspectInstance);

        currentInspectable?.OnInspectionEnd();
        currentInspectable = null;

        inspectionActions.Disable();
        playerActions.Enable();

        inspectionCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // =========================
        // UI / 연출 종료 알림
        // =========================
        EventBus.Publish(new InspectionEndedEvent());
    }

    #endregion

    #region Rotation

    private void HandleRotationInput()
    {
        if (!rotateHold.IsPressed())
            return;

        Vector2 delta = rotate.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 0.001f)
            return;

        yaw += delta.x * rotateSpeed;
        pitch -= delta.y * rotateSpeed;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        inspectPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ResetRotation()
    {
        ResetRotationInternal();
    }

    private void ResetRotationInternal()
    {
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;
    }

    #endregion

    #region Inspect Object

    private void SpawnInspectObject(GameObject source)
    {
        if (source == null)
        {
            Debug.LogWarning("[InspectionManager] Inspect source is null.");
            return;
        }

        inspectInstance = Instantiate(source, inspectPivot);
        inspectInstance.transform.localPosition = Vector3.zero;
        inspectInstance.transform.localRotation = Quaternion.identity;
        inspectInstance.transform.localScale = Vector3.one;
    }

    #endregion
}

