using UnityEngine;

public class CellDoorController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string cellId;

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellAnchorRegistry anchorRegistry;

    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Door Feedback")]
    [SerializeField] private bool verboseLog = true;

    [Header("Debug")]
    [SerializeField] private bool animationTestOnly = true;

    // 감방 내부/외부 판정용(간단 구현)
    private bool _playerInsideThisCell;

    private void Awake()
    {
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
        if (anchorRegistry == null) anchorRegistry = FindObjectOfType<CellAnchorRegistry>();
    }

    // 감방 내부 트리거(감방 프리팹 안에 BoxCollider IsTrigger 1개 두고 연결)
    public void SetPlayerInside(bool inside) => _playerInsideThisCell = inside;

    /// <summary>
    /// 상호작용 키(E 등)에서 호출하세요.
    /// </summary>
    public void Interact()
    {
        if (animationTestOnly)
            return;

        if (inspection == null || cellManager == null || anchorRegistry == null || playerRoot == null)
        {
            Debug.LogWarning("[Door] Missing refs.");
            return;
        }

        if (!_playerInsideThisCell)
        {
            TryEnter();
        }
        else
        {
            TryExit();
        }
    }

    private void TryEnter()
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return;

        // 오늘 잠긴 방이면 입장 불가
        if (cell.IsLockedForDay)
        {
            if (verboseLog) Debug.Log($"[Door] Enter blocked (LockedForDay) cell={cellId}");
            // TODO: 잠김 UI/SFX
            return;
        }

        bool ok = inspection.TryEnterCell(cellId);
        if (!ok)
        {
            if (verboseLog) Debug.Log($"[Door] TryEnter failed cell={cellId}");
            return;
        }

        if (verboseLog) Debug.Log($"[Door] Enter SUCCESS cell={cellId}");
        // TODO: 문 열림/닫힘 연출, 철창 SFX
    }

    private void TryExit()
    {
        bool ok = inspection.RequestExitCell(cellId);
        if (!ok)
        {
            if (verboseLog) Debug.Log($"[Door] Exit blocked cell={cellId}");
            // TODO: 잠김 UI/SFX (진압 중 성공 전)
            return;
        }

        if (verboseLog) Debug.Log($"[Door] Exit SUCCESS cell={cellId}");
        // TODO: 문 연출, 철창 SFX
    }
}
