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
    [Tooltip("수상한 방 진압 성공")]
    [SerializeField] private int suspiciousSuppressSuccessDelta = -5;

    [Tooltip("정상 방 경고/무시 성공")]
    [SerializeField] private int normalIgnoreSuccessDelta = -5;

    [Tooltip("수상한 방 경고/무시 실패")]
    [SerializeField] private int suspiciousIgnoreFailDelta = +10;

    [Tooltip("정상 방 과잉 진압 실패")]
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
    /// 하루 정산 적용 (게이지 로직)
    /// </summary>
    public void ApplyDailyReport(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        int delta = 0;

        // 1. 점검 완료된 방 처리
        foreach (var r in resolved)
        {
            if (r.isSuspicious)
            {
                if (r.didSuppress) delta += suspiciousSuppressSuccessDelta; // 수상 + 진압 성공
                else delta += suspiciousIgnoreFailDelta; // 수상 + 경고(실패)
            }
            else
            {
                if (r.didSuppress) delta += normalSuppressFailDelta; // 정상 + 과잉 진압
                else delta += normalIgnoreSuccessDelta; // 정상 + 경고 성공
            }
        }

        // 2. 미점검 방 처리 (전부 실패 취급)
        foreach (var u in uninspected)
        {
            if (u.isSuspicious) delta += suspiciousIgnoreFailDelta;
            else delta += normalSuppressFailDelta;
        }

        // 3. 게이지 적용
        riotGauge += delta;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);
        GameManager.Instance.AddRiotGauge(delta);

        if (GameManager.Instance != null)
            GameManager.Instance.SetRiotGauge(riotGauge);

        Debug.Log($"[Settlement] RiotGauge Δ={delta}, Result={riotGauge}/{maxRiotGauge}");
    }

    /// <summary>
    /// UI 표시용 정산 데이터 생성
    /// </summary>
    public SettlementUIData BuildSettlementData(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        SettlementUIData data = new SettlementUIData();

        // 1. 플레이어 조치 결과 집계 (기존 유지)
        foreach (var r in resolved)
        {
            if (r.didSuppress) data.SuppressedCount++;
            else data.WarnedCount++;
        }
        data.UncheckedCount = uninspected.Count;

        // 2. [변경] 층별 '활성 감방' 개수 집계
        if (_cellManager != null)
        {
            foreach (var cell in _cellManager.Cells)
            {
                // [수정 전] if (cell.IsSuspicious) // 이상현상만 집계했었음

                // [수정 후] 오늘 활성화된(배정된) 모든 감방을 집계
                if (cell.IsActiveToday)
                {
                    if (cell.Floor == 1)
                        data.Floor1_ActiveCount++;
                    else if (cell.Floor == 2)
                        data.Floor2_ActiveCount++;
                }
            }
        }

        return data;
    }

    public void ApplyDailyBaseIncrease()
    {
        riotGauge += dailyBaseIncrease;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);
        GameManager.Instance.AddRiotGauge(dailyBaseIncrease);

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