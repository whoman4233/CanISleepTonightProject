using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class TestInspectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform inspectPivot;
    [SerializeField] private Volume inspectionVolume;

    [Header("TEST ONLY")]
    [SerializeField] private GameObject testInspectPrefab;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 0.1f;

    private bool isInspecting;
    private GameObject currentInspectObject;

    private PlayerInputs playerInputs;
    private PlayerInputs.PlayerActions playerActions;
    private PlayerInputs.InspectionActions inspectionActions;

    private InputAction rotateDelta;
    private InputAction rotateHold;
    private InputAction exitAction;

    private void Start()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("[TestInspectionManager] PlayerController를 찾을 수 없습니다.");
            return;
        }

        playerInputs = playerController.playerInput;

        if (playerInputs == null)
        {
            Debug.LogError("[TestInspectionManager] PlayerInputs가 초기화되지 않았습니다.");
            return;
        }

        playerActions = playerInputs.Player;
        inspectionActions = playerInputs.Inspection;

        rotateDelta = inspectionActions.Rotate;
        rotateHold = inspectionActions.RotateHold;
        exitAction = inspectionActions.Exit;

        exitAction.performed += _ => ExitInspection();
    }

    private void Update()
    {
        // TEST 진입 키
        if (!isInspecting && Keyboard.current.fKey.wasPressedThisFrame)
        {
            EnterInspection(testInspectPrefab);
        }

        if (!isInspecting)
            return;

        if (!rotateHold.IsPressed())
            return;

        Vector2 delta = rotateDelta.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 0.001f)
            return;

        inspectPivot.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
        inspectPivot.Rotate(Vector3.right, delta.y * rotateSpeed, Space.Self);
    }

    private void EnterInspection(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[TestInspectionManager] Inspect Prefab이 비어 있습니다.");
            return;
        }

        isInspecting = true;

        playerActions.Disable();
        inspectionActions.Enable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        inspectPivot.localRotation = Quaternion.identity;

        currentInspectObject = Instantiate(prefab, inspectPivot);
        currentInspectObject.transform.localPosition = Vector3.zero;
        currentInspectObject.transform.localRotation = Quaternion.identity;

        if (inspectionVolume != null)
            inspectionVolume.weight = 1f;
    }

    private void ExitInspection()
    {
        if (!isInspecting)
            return;

        isInspecting = false;

        if (currentInspectObject != null)
            Destroy(currentInspectObject);

        if (inspectionVolume != null)
            inspectionVolume.weight = 0f;

        inspectionActions.Disable();
        playerActions.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}


