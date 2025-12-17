using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolInteractor : MonoBehaviour
{
    private PlayerManager playerManager;
    private PrisonCellManager cellManager;
    private GameManager gameManager;

    private void Start()
    {
        GameContext context = GameContext.Instance;
        playerManager = context.Get<PlayerManager>();
        cellManager = context.Get<PrisonCellManager>();
        gameManager = context.Get<GameManager>();
    }

    private void Update()
    {
        if(gameManager.CurrentPhase != GamePhase.Patrol)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.E)) // 일단 레거시로 해놓음 추후 input시스템으로 변경 가능
        {
            PrisonInteraction();
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
        if(cellRuntime != null)
        {
            if(cellRuntime.IsActiveToday && !playerManager.IsObserving)
            {
                StartInspection(cellRuntime);
            }
        }
    }

    private void StartInspection(CellRuntime data)
    {
        data.IsInspectingNow = true;
        playerManager.SetObserving(true); // 점검 끝나면 둘 다 false로 돌려줄것
    }

    public void EndInspection() // 점검 끝나는 시점에 호출
    {
        string currentId = playerManager.CurrentCellID;
        if (string.IsNullOrEmpty(currentId)) return;

        CellRuntime data = cellManager.GetCell(currentId);
        if (data != null)
        {
            data.IsInspectingNow = false;
        }
        playerManager.SetObserving(false);
    }

    private void OnTriggerEnter(Collider other) // 어느 감방 앞에 있는지 체크. 추후 확인방식 변경 가능.
    {
        if (other.CompareTag("PrisonCell"))
        {
            playerManager.SetInspectingCell(other.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PrisonCell"))
        {
            playerManager.SetInspectingCell(string.Empty);
        }
    }
}
