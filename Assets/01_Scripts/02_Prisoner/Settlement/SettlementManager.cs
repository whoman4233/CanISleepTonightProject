using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하루 정산 담당
/// - 점검 결과를 받아 폭동 게이지 증감 처리
/// - UI 표시에 필요한 정산 데이터 집계 기능 제공
/// </summary>
public class SettlementManager : MonoBehaviour
{
    [Header("Riot Gauge")]
    //[SerializeField] private int riotGauge = 30;
    //[SerializeField] private int maxRiotGauge = 100;

    [Header("Gauge Change Values")]
    [Tooltip("수상한 방 진압 성공 (감소)")]
    [SerializeField] private int suspiciousSuppressSuccessDelta = -5;

    [Tooltip("정상 방 경고/무시 성공 (감소)")]
    [SerializeField] private int normalIgnoreSuccessDelta = -5;

    [Tooltip("수상한 방 경고/무시 실패 (증가)")]
    [SerializeField] private int suspiciousIgnoreFailDelta = +10;

    [Tooltip("정상 방 과잉 진압 실패 (증가)")]
    [SerializeField] private int normalSuppressFailDelta = +10;

    [Header("Daily Base Increase (Standby)")]
    [SerializeField] private int dailyBaseIncrease = 20;

    private int riotGauge;
    private int maxRiotGauge;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    // 층별 데이터 집계를 위한 참조
    private PrisonCellManager _cellManager;

    private void Awake()
    {
        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                ApplyDailyBaseIncrease();
                Debug.Log("SettlementManager의 ApplyDailyBaseIncrease 완료");
            }
        };
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            riotGauge = GameManager.Instance.RiotGauge;
            maxRiotGauge = GameManager.Instance.MaxRiotGauge;
            Debug.Log($"데이터 로드 완료: {riotGauge}/{maxRiotGauge}");
        }
        else
        {
            Debug.LogError("GameManager를 찾을 수 없습니다");
        }

        // 층별 이상현상 집계를 위해 매니저 찾기
        _cellManager = FindObjectOfType<PrisonCellManager>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    /// <summary>
    /// 하루 정산 적용 (실제 게임 데이터 반영)
    /// </summary>
    public void ApplyDailyReport(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        // 1. 점수 계산 (공통 함수 사용)
        int delta = CalculateDelta(resolved, uninspected);

        // 2. 게이지 적용
        riotGauge += delta;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);

        // GameManager 업데이트
        // (주의: AddRiotGauge를 쓰면 delta만큼 더해지고, SetRiotGauge는 값을 덮어씁니다. 중복 호출 방지 확인 필요)
        // 여기서는 명확하게 Set으로 최종값을 맞춥니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetRiotGauge(riotGauge);
        }

        Debug.Log($"[Settlement] RiotGauge Δ={delta}, Result={riotGauge}/{maxRiotGauge}");
    }

    /// <summary>
    /// UI 표시용 정산 데이터 생성
    /// </summary>
    public SettlementUIData BuildSettlementData(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        SettlementUIData data = new SettlementUIData();

        // 1. 플레이어 조치 결과 집계
        foreach (var r in resolved)
        {
            if (r.didSuppress) data.SuppressedCount++;
            else data.WarnedCount++;
        }
        data.UncheckedCount = uninspected.Count;

        // 2. 층별 '활성 감방' 개수 집계
        if (_cellManager != null)
        {
            foreach (var cell in _cellManager.Cells)

            {
                if (cell.IsActiveToday)
                {
                    if (cell.Floor == 1)
                        data.Floor1_ActiveCount++;
                    else if (cell.Floor == 2)
                        data.Floor2_ActiveCount++;
                }
            }
        }

        // 3. [핵심 수정] UI에 표시할 게이지 변화량 할당 (이 부분이 빠져 있어서 0으로 나왔던 것임)
        data.RiotGaugeDelta = CalculateDelta(resolved, uninspected);

        return data;
    }

    /// <summary>
    /// 점수(게이지 변화량) 계산 로직 (중복 방지용 헬퍼 함수)
    /// </summary>
    private int CalculateDelta(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        int delta = 0;

        // 1. 해결된 방 점수 계산
        foreach (var r in resolved)
        {
            if (r.isSuspicious)
            {
                if (r.didSuppress) delta += suspiciousSuppressSuccessDelta; // 성공 (감소)
                else delta += suspiciousIgnoreFailDelta; // 실패 (증가)
            }
            else
            {
                if (r.didSuppress) delta += normalSuppressFailDelta; // 과잉진압 (증가)
                else delta += normalIgnoreSuccessDelta; // 성공 (감소)
            }
        }

        // 2. 미점검 방 점수 계산 (전부 실패 취급)
        foreach (var u in uninspected)
        {
            if (u.isSuspicious) delta += suspiciousIgnoreFailDelta;
            else delta += normalSuppressFailDelta;
        }

        return delta;
    }

    public void ApplyDailyBaseIncrease()
    {
        riotGauge += dailyBaseIncrease;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);

        if (GameManager.Instance != null)
            GameManager.Instance.SetRiotGauge(riotGauge);

        Debug.Log($"[Standby] RiotGauge +{dailyBaseIncrease} => {riotGauge}/{maxRiotGauge}");
    }

    public void SetRiotGauge(int value)
    {
        riotGauge = value;
        Debug.Log($"폭동 게이지가 로드된 값으로 설정됨: {value}");
    }

    public bool IsRiotOver()
    {
        return riotGauge >= maxRiotGauge;
    }
}

/// <summary>
/// 정산 결과 UI 표시용 데이터 구조체
/// </summary>
[System.Serializable]
public struct SettlementUIData
{
    [Header("Floor Active Counts")]
    public int Floor1_ActiveCount; // [변경] 1층 활성화된 감방 개수
    public int Floor2_ActiveCount; // [변경] 2층 활성화된 감방 개수

    [Header("Player Actions")]
    public int SuppressedCount;
    public int WarnedCount;
    public int UncheckedCount;

    public float RiotGaugeDelta;
}