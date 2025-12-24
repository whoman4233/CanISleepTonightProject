using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하루 정산 담당
/// - 점검 결과를 받아 폭동 게이지 증감 처리
/// - 수치는 전부 SerializeField로 분리
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

    private GameManager gameManager;
    private int riotGauge;
    private int maxRiotGauge;

    //public int RiotGauge => riotGauge;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

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
        // GameManager.Instance가 확실히 존재할 때 값을 가져옵니다.
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
    /// 하루 정산 적용
    /// </summary>
    public void ApplyDailyReport(
        List<ResolvedRecord> resolved,
        List<UninspectedRecord> uninspected)
    {
        int delta = 0;

        // 1. 점검 완료된 방 처리
        foreach (var r in resolved)
        {
            if (r.isSuspicious)
            {
                if (r.didSuppress)
                {
                    // 수상 + 진압 성공
                    delta += suspiciousSuppressSuccessDelta;
                }
                else
                {
                    // 수상 + 경고(실패)
                    delta += suspiciousIgnoreFailDelta;
                }
            }
            else
            {
                if (r.didSuppress)
                {
                    // 정상 + 과잉 진압
                    delta += normalSuppressFailDelta;
                }
                else
                {
                    // 정상 + 경고 성공
                    delta += normalIgnoreSuccessDelta;
                }
            }
        }

        // 2. 미점검 방 처리 (전부 실패 취급)
        foreach (var u in uninspected)
        {
            if (u.isSuspicious)
            {
                // 수상 + 미점검
                delta += suspiciousIgnoreFailDelta;
            }
            else
            {
                // 정상 + 미점검
                delta += normalSuppressFailDelta;
            }
        }

        // 3. 게이지 적용
        riotGauge += delta;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);

        Debug.Log($"[Settlement] RiotGauge Δ={delta}, Result={riotGauge}/{maxRiotGauge}");
    }

    public bool IsRiotOver()
    {
        return riotGauge >= maxRiotGauge;
    }

    public void ResetGauge(int startValue)
    {
        riotGauge = Mathf.Clamp(startValue, 0, maxRiotGauge);
    }

    [Header("Daily Base Increase (Standby)")]
    [SerializeField] private int dailyBaseIncrease = 20;

    public void ApplyDailyBaseIncrease()
    {
        riotGauge += dailyBaseIncrease;
        riotGauge = Mathf.Clamp(riotGauge, 0, maxRiotGauge);
        Debug.Log($"[Standby] RiotGauge +{dailyBaseIncrease} => {riotGauge}/{maxRiotGauge}");
    }
    public void SetRiotGauge(int value)
    {
        riotGauge = value; // 내부 변수에 로드된 값 할당
        Debug.Log($"폭동 게이지가 로드된 값으로 설정됨: {value}");
    }
}
