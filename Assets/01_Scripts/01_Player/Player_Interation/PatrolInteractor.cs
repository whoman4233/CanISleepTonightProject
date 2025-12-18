using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolInteractor : MonoBehaviour
{
    private PlayerManager playerManager;
    private PrisonCellManager cellManager;
    private GameManager gameManager;

    [Header("레이캐스트 세팅")]
    [SerializeField] private float rayDistance = 3f; // 레이 길이
    [SerializeField] private LayerMask prisonCellLayer; // 감방 오브젝트 레이어

    private CellRuntime currentActiveData;

    private void Start()
    {
        GameContext context = GameContext.Instance;
        playerManager = context.Get<PlayerManager>();
        cellManager = context.Get<PrisonCellManager>();
        gameManager = context.Get<GameManager>();
    }

    private void Update()
    {
        if (gameManager.CurrentPhase != GamePhase.Patrol)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.E)) // 일단 레거시로 해놓음 추후 input시스템으로 변경 가능
        {
            if (playerManager.IsObserving) // 점검중이면 점검끝내고 
            {
                EndInspection();
                Debug.Log("점검 종료");
            }
            else
            {
                PrisonInteraction(); // 점검중 아니면 점검 시작
                Debug.Log("점검 시작");
            }
        }

    }

    private void PrisonInteraction()
    {
        string currentId = playerManager.GetCurrentCellID();
        if (string.IsNullOrEmpty(currentId))
        {
            return;
        }
        CellRuntime cellRuntime = cellManager.GetCell(currentId); // 2. PrisonCellManager에서 CellRuntime를 가져옴
        if (cellRuntime != null)
        {
            if (cellRuntime.IsActiveToday && !playerManager.IsObserving && !cellRuntime.WasResolvedToday) // 활성화 된 방, 검사중이 아닌 방, 검사 안한 방만 검사할 수 있게
            {
                StartInspection(cellRuntime);
            }
        }
    }

    private void StartInspection(CellRuntime data)
    {
        currentActiveData = data;
        data.IsInspectingNow = true;
        data.State = CellState.Inspecting; // 점검중으로 변경
        playerManager.SetObserving(true); // 점검 끝나면 둘 다 false로 돌려줄것
    }

    public void EndInspection()
    {
        if (currentActiveData != null)
        {
            //점검 종료 시 필드 정리
            currentActiveData.IsInspectingNow = false;

            // 만약 UI에서 진압(Suppress)이 이루어지지 않고 그냥 문을 닫았다면 Inactive로 복구
            if (!currentActiveData.WasResolvedToday)
            {
                currentActiveData.State = CellState.Inactive;
            }
            else
            {
                // 이미 해결되었다면 오늘 잠금 상태로 변경
                currentActiveData.State = CellState.LockedForDay;
                currentActiveData.IsLockedForDay = true;
            }

            currentActiveData = null; // 점검 끝
        }

        playerManager.SetObserving(false);
    }
    
    //상호작용 레이검사 추후 추가
 
}
