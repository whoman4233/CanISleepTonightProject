using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private PlayerController playerController;
    private GameManager gameManager;
    private PrisonCellManager prisonCellManager;
    public struct InspectionRecord
    {
        public bool IsInspected { get; private set; }      // 입장했는지 (미확인 방 페널티 체크용)     //순찰 관련 다른 스크립트로 싹 빼는게 좋을듯
        public bool WasSuppressed { get; private set; }    // 진압을 시도했는지 (게이지 증감 규칙 체크용)

        public InspectionRecord(bool inspected, bool suppressed)
        {
            IsInspected = inspected;
            WasSuppressed = suppressed;
        }
    }

    [Header("순찰 및 점검 상태")]  // Key: 감방 ID, Value: 플레이어의 행동 기록

    private Dictionary<int, InspectionRecord> dailyInspectionRecords = new Dictionary<int, InspectionRecord>();

    public int CurrentInspectingCellID { get; private set; } = 0; // 0이면 점검 중인 방 없음
    public bool IsObserving { get; private set; } = false;      // 관찰 중 상태 (판단 대기)
    public bool IsEngagingInAction { get; private set; } = false; // 진압 액션 중

    public void Initialize()
    {
        GameContext context = GameContext.Instance;
        playerController = context.Get<PlayerController>();
        gameManager = context.Get<GameManager>();
        prisonCellManager = context.Get<PrisonCellManager>();

        if(gameManager == null || prisonCellManager == null)
        {
            Debug.LogError("PlayerController 혹은 GameManager가 연결되지 않았습니다");
            return;
        }
        SetMovementState(false);
    }

    public void SetMovementState(bool state) // 플레이어 이동 가능 상태
    {
        playerController.SetMovementEnabled(state);
    }

    public void ResetDailyRecoed() // 플레이어 상태 초기화
    {
        dailyInspectionRecords.Clear();
        CurrentInspectingCellID = 0;
        IsObserving = false;
        IsEngagingInAction = false;
        Debug.Log("플레이어 순찰 상태 초기화 완료");
    }

    public void RequestPatrolStart() // 문 상호작용 시 호출(순찰 시작)
    {
        gameManager.StartPatrolLogic();
    }

    public void RequestPatrolEnd() // 문 상호작용 시 호출 (순찰 끝)
    {
        gameManager.EndPatrolLogic();
    }
}
