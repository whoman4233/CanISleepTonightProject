using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public struct InspectionRecord
    {
        public bool IsInspected { get; private set; }      // 입장했는지 (미확인 방 페널티 체크용)
        public bool WasSuppressed { get; private set; }    // 진압을 시도했는지 (게이지 증감 규칙 체크용)

        public InspectionRecord(bool inspected, bool suppressed)
        {
            IsInspected = inspected;
            WasSuppressed = suppressed;
        }
    }

    [Header("플레이어 스테이터스")]
    [SerializeField] private int maxHp = 50;
    [SerializeField] private int currentHp;
    [SerializeField] private int baseAtk = 10;

    [Header("순찰 및 점검 상태")]  // Key: 감방 ID, Value: 플레이어의 행동 기록

    private Dictionary<int, InspectionRecord> dailyInspectionRecords = new Dictionary<int, InspectionRecord>();

    public int CurrentInspectingCellID { get; private set; } = 0; // 0이면 점검 중인 방 없음
    public bool IsObserving { get; private set; } = false;      // 관찰 중 상태 (판단 대기)
    public bool IsEngagingInAction { get; private set; } = false; // 진압 액션 중

    public void Initialize()
    {
        // bootstrap 추가 이후
    }
}
