using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("요소")]
    [SerializeField] private Transform inspectPivot; //회전 값
    [SerializeField] private Volume inspectionVolume; // 블러처리
    [SerializeField] private PlayerInput playerInput; // PlayerInput 참조

    [Header("회전 세팅")]
    [SerializeField] private float rotateSpeed = 0.1f; //회전 속도
    [SerializeField] private float maxVerticalAngle = 80f; // 상하 회전 제한

    private GameObject currentInspectObject;
    private bool isInspecting;

    // Input Actions
    private InputAction rotateDeltaAction;
    private InputAction rotateHoldAction;
    private InputAction exitAction;

    private float currentVerticalAngle = 0f; // 회전 누적 값

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Inspection Action Map 캐싱
        var inspectionMap = playerInput.actions.FindActionMap("Inspection");

        rotateDeltaAction = inspectionMap.FindAction("RotateDelta");
        rotateHoldAction = inspectionMap.FindAction("RotateHold");
        exitAction = inspectionMap.FindAction("Exit");

        exitAction.performed += _ => ExitInspection();
    }

    private void Update()
    {
        if (!isInspecting)
            return;

        // 왼쪽 클릭을 누르고 있을 때만 회전
        if (!rotateHoldAction.IsPressed())
            return;

        Vector2 delta = rotateDeltaAction.ReadValue<Vector2>();

        if (delta.sqrMagnitude < 0.001f)
            return;

        RotateObject(delta);
    }

    private void RotateObject(Vector2 delta)
    {
        // 좌우 회전 (월드 기준 Y축)
        inspectPivot.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);

        // 상하 회전 (로컬 기준 X축)
        float verticalDelta = delta.y * rotateSpeed;
        float nextAngle = currentVerticalAngle + verticalDelta;

        if (Mathf.Abs(nextAngle) > maxVerticalAngle)
            return;

        currentVerticalAngle = nextAngle;
        inspectPivot.Rotate(Vector3.right, verticalDelta, Space.Self);
    }

    // ============================
    // Inspection 진입
    // ============================
    public void EnterInspection(GameObject inspectPrefab)
    {
        if (isInspecting)
            return;

        isInspecting = true;

        // Player 입력 비활성화 / Inspection 입력 활성화
        playerInput.actions.FindActionMap("Player").Disable();
        playerInput.actions.FindActionMap("Inspection").Enable();

        // 플레이어 조작 차단
        //GameManager.Instance.SetCanMove(false);
        //PlayerManager.Instance.Player.controller.canLook = false;

        // 커서 활성화
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 회전 값 초기화
        currentVerticalAngle = 0f;
        inspectPivot.localRotation = Quaternion.identity;

        // Inspect 오브젝트 생성
        currentInspectObject = Instantiate(inspectPrefab, inspectPivot);
        currentInspectObject.transform.localPosition = Vector3.zero;
        currentInspectObject.transform.localRotation = Quaternion.identity;

        // 블러 활성화
        if (inspectionVolume != null)
            inspectionVolume.weight = 1f;
    }

    // ============================
    // Inspection 종료
    // ============================
    public void ExitInspection()
    {
        if (!isInspecting)
            return;

        isInspecting = false;

        if (currentInspectObject != null)
            Destroy(currentInspectObject);

        // 블러 해제
        if (inspectionVolume != null)
            inspectionVolume.weight = 0f;

        // 입력 복구
        playerInput.actions.FindActionMap("Inspection").Disable();
        playerInput.actions.FindActionMap("Player").Enable();

        // 플레이어 조작 복구
        //GameManager.Instance.SetCanMove(true);
        //PlayerManager.Instance.Player.controller.canLook = true;

        // 커서 복구
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}