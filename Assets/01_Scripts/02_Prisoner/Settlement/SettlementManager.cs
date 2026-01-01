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
    // GameManager에서 가져오므로 SerializeField 제거됨

    [Header("Gauge Change Values")]
    [Tooltip("수상한 방 진압 성공 (감소)")]
    [SerializeField] private int suspiciousSuppressSuccessDelta = -5;

    [Tooltip("정상 방 경고/무시 성공 (감소)")]
    [SerializeField] private int normalIgnoreSuccessDelta = -5;

    [Tooltip("수상한 방 경고/무시 실패 (증가)")]
    [SerializeField] private int suspiciousFailDelta = +10;

    [Tooltip("정상 방 과잉 진압 실패 (증가)")]
    [SerializeField] private int normalSuppressFailDelta = +10;

    [Tooltip("이상방 무시")]
    [SerializeField] private int suspiciousIgnoreDelta = +10;

    [Tooltip("정상방 무시")]
    [SerializeField] private int normalIgnoreDelta = +10;


    [Header("Daily Base Increase (Standby)")]
    [SerializeField] private int dailyBaseIncrease = 20;

    private int riotGauge;
    private int maxRiotGauge;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    // [변경] PrisonManager 참조
    private PrisonManager _prisonManager;

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

        // [변경] PrisonManager 찾기
        _prisonManager = FindObjectOfType<PrisonManager>();
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

        // 2. [최적화] 층별 '활성 감방' 개수 집계
        // PrisonManager가 이미 계산해둔 값을 가져옵니다. 반복문 불필요.
        if (_prisonManager != null)
        {
            data.Floor1_ActiveCount = _prisonManager.ActiveCell1f;
            data.Floor2_ActiveCount = _prisonManager.ActiveCell2f;
        }

        // 3. UI에 표시할 게이지 변화량 할당
        data.RiotGaugeDelta = CalculateDelta(resolved, uninspected);

        return data;
    }

    /// <summary>
    /// 점수(게이지 변화량) 계산 로직
    /// </summary>
    private int CalculateDelta(List<ResolvedRecord> resolved, List<UninspectedRecord> uninspected)
    {
        int delta = 0;

        // 1. 해결된 방 (Resolved) 점수 계산
        foreach (var r in resolved)
        {
            if (r.isSuspicious)
            {
                // 수상한 방 -> 진압했으면 성공(-), 무시했으면 실패(+)
                if (r.didSuppress) delta += suspiciousSuppressSuccessDelta;
                else delta += suspiciousFailDelta;
            }
            else
            {
                // 정상 방 -> 진압했으면 과잉진압(+), 무시했으면 성공(-)
                if (r.didSuppress) delta += normalSuppressFailDelta;
                else delta += normalIgnoreSuccessDelta;
            }
        }

        // 2. 미점검 방 (Uninspected) 점수 계산 - 🔥 [수정됨]
        foreach (var u in uninspected)
        {
            if (u.isSuspicious)
            {
                // 수상한 방을 안 보고 넘어감 -> 큰일 남! (전용 패널티 적용)
                delta += suspiciousIgnoreDelta;
            }
            else
            {
                // 정상인 방을 안 보고 넘어감 -> 소폭 증가 or 0 (기획에 따라 설정)
                delta += normalIgnoreDelta;
            }
        }

        return delta;
    }

    public void ApplyDailyBaseIncrease()
    {
        // Standby 페이즈에 하루 기본 증가량 적용
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
    [Header("Day")]
    public int ReportCurrentDay;

    [Header("Floor Active Counts")]
    public int Floor1_ActiveCount; 
    public int Floor2_ActiveCount; 

    [Header("Player Actions")]
    public int SuppressedCount;
    public int WarnedCount;
    public int UncheckedCount;

    public float RiotGaugeDelta;
}