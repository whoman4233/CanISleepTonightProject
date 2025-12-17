using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("페이즈 상태")]
    [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
    public GamePhase CurrentPhase => currentPhase;
    [SerializeField] private int currentDay = 0;

    //private float riotGauge; // 폭동게이지 혹시몰라서 float로 해 놓음.

    public event Action<GamePhase> OnPhaseChanged; // 페이즈 변경 이벤트
    public event Action<float> OnRiotGaugeChanged; // 폭동게이지 변경 이벤트
    public event Action OnGameOver; // 게임오버 이벤트

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 480f; // 480초(현실)
    private const float PatrolDisplayHours = 8.0f; // 8시간(인게임) 나중에 ui에 연결할 수 있게 수치 조정 및 시간 감소 로직 필요
    private float currentInGameSeconds = 3600 * 8; // 현재 인게임 시간
    public event Action<float> OnInGameTimeUpdated; // 타이머 관련 ui이벤트 

    private PlayerManager playerManager;
    private PrisonCellManager prisonCellManager;
    private SettlementManager settlementManager;
    private SettlementReportBuilder settlementReportBuilder;

    public void Initialize() // Bootstrap에서 GameContext.RegisterService<GameManager>(this) 이후 호출
    {
        GameContext context = GameContext.Instance;

        playerManager = context.Get<PlayerManager>();
        prisonCellManager = context.Get<PrisonCellManager>();
        settlementManager = context.Get<SettlementManager>();
        settlementReportBuilder = context.Get<SettlementReportBuilder>();
        Debug.Log($"게임매니저 초기화 완료. \n 현재 폭동수지{settlementManager.RiotGauge}");
        currentInGameSeconds = PatrolDisplayHours * 3600f; // 3600을 곱하여 초 단위로 계산
        OnInGameTimeUpdated?.Invoke(currentInGameSeconds);
    }

    public void ChangePhase(GamePhase newPhase)
    {
        Debug.Log($"{CurrentPhase} 에서 {newPhase}로 페이즈 전환이 이루어졌습니다.");
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);

        // =========================
        // [추가] UI 및 외부 시스템 전파용
        // =========================
        EventBus.Publish(new GamePhaseChangedEvent(newPhase));

        switch (newPhase)
        {
            case GamePhase.Standby: 
                OnEnterStandby(); 
                break;
            case GamePhase.Briefing:
                OnEnterBriefing();
                break;
            case GamePhase.Patrol:
                OnEnterPatrol();
                break;
            case GamePhase.Settlement:
                OnEnterSettlement();
                break;
            case GamePhase.OffDuty:
                OnEnterOffDuty();
                break;
            case GamePhase.Ending:
                OnEnterEnding();
                break;
            default:
                break;
        }
    }
    private void OnEnterStandby() // 준비 페이즈
    {
        if(currentPhase == GamePhase.Ending)
        {
            return;
        }
        playerManager.ResetDailyRecoed(); // 상태
        prisonCellManager.RunStandbySetup(); // 죄수 초기화
        settlementReportBuilder.ClearResolvedCache(); // 점검 기록 초기화
        settlementManager.ApplyDailyBaseIncrease(); // 일일 폭동게이지 증가
        OnRiotGaugeChanged?.Invoke(settlementManager.RiotGauge);
        currentDay++;
        // 랜덤 감방
        //playerManager.SetMovementState(false);
        ChangePhase(GamePhase.Briefing);
    }
    private void OnEnterBriefing() // 브리핑 페이즈
    {
        ChangePhase(GamePhase.Patrol); // 임시조치
    }

    private void OnEnterPatrol() // 순찰 페이즈
    {
        StopAllCoroutines();
        //playerManager.SetMovementState(true);
        StartCoroutine(UpdateTimer()); // 타이머 코루틴 시작
        StartCoroutine(AutoTransitionAfterDelay(patrolDurationSeconds, GamePhase.Settlement)); // 480초 후 자동으로 페이즈 종료 및 전환
    }

    private void OnEnterSettlement() // 정산 페이즈
    {
        //playerManager.SetMovementState(false);
        //폭동게이지 계산 추가
        settlementReportBuilder.RunSettlement();
        OnRiotGaugeChanged?.Invoke(settlementManager.RiotGauge);
        Debug.Log($"현재 폭동 수치 {settlementManager.RiotGauge}");
        StartCoroutine(AutoTransitionAfterDelay(3.0f, GamePhase.OffDuty)); //어떤식으로 페이즈 이동할것인가
    }

    private void OnEnterOffDuty() // 퇴근 페이즈
    {
        //오토세이브
        //폭동게이지에 따라 n일차 시작 혹은 엔딩
        if(currentDay == 7)
        {
            ChangePhase(GamePhase.Ending);
        }
    }

    private void OnEnterEnding() // 엔딩 페이즈
    {
        Debug.Log("엔딩 페이즈 진입");

    }

    private IEnumerator AutoTransitionAfterDelay(float delay, GamePhase nextPhase)
    {
        yield return new WaitForSeconds(delay);
        ChangePhase(nextPhase);
    }

    private IEnumerator UpdateTimer() // 타이머 코루틴
    {
        yield return new WaitForSeconds(1.0f);
        while(CurrentPhase == GamePhase.Patrol && currentInGameSeconds > 0 && patrolDurationSeconds > 0)
        {
            float realSeconds = 1.0f; // 현실 1초당 인게임 1분
            currentInGameSeconds -= (realSeconds * 60f); // 현실 1초당 60초 감소 ui적용 시 부자연스러우면 델타타임으로 변경 가능
                                                              // 1시간 = 3600초 이니까 60 단위로 쪼개서 시, 분, 초 등으로 나눠서 활용 가능함.
            if (currentInGameSeconds < 0)
            {
                currentInGameSeconds = 0;
            }
            OnInGameTimeUpdated?.Invoke(currentInGameSeconds);
            yield return null;
        }
    }

    public void StartDayLogic() // 일일 초기화
    {
        if(currentPhase == GamePhase.Ending)
        {
            return;
        }
        ChangePhase(GamePhase.Standby);
        Debug.Log("일일 초기화 완료");
    }


    public void StartPatrolLogic() 
    {
        if(currentPhase != GamePhase.Briefing)
        {
            Debug.LogWarning("브리핑페이즈가 아닙니다");
            return;
        }
        Debug.Log("순찰 시작");
        ChangePhase(GamePhase.Patrol);
    }

    public void EndPatrolLogic()
    {
        if (currentPhase == GamePhase.Patrol)
        {
            Debug.Log("순찰 종료");
            ChangePhase(GamePhase.Settlement);
        }
    }
}
