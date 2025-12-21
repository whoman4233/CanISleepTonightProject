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

    private Action<RequestPhaseChangeEvent> _requestPhaseChange;
    private Action<EndingConditionMetEvent> _onEndingConditionMet;

    [Header("엔딩 설정")]
    private GameEndingType finalEnding = GameEndingType.None;

    public event Action<GamePhase> OnPhaseChanged; // 페이즈 변경 이벤트
    public event Action<float> OnRiotGaugeChanged; // 폭동게이지 변경 이벤트
    public event Action<GameEndingType> OnGameEnded; // 게임엔딩 이벤트

    [Header("순찰 페이즈 타임어택")]
    [SerializeField] private float patrolDurationSeconds = 4f; // 480초
    public event Action<float> OnInGameTimeUpdated; // 타이머 관련 ui이벤트 

    private void Awake()
    {
        _requestPhaseChange = (e) => ChangePhase(e.TargetPhase); // 페이즈 변경 요청시 바로 변경
        _onEndingConditionMet = e => // 엔딩 조건 충족하면 엔딩페이즈 이동 및 엔딩타입 받아옴
        {
            finalEnding = e.EndingType;
            ChangePhase(GamePhase.Ending);
        };
    }
    public void Initialize() // Bootstrap에서 GameContext.RegisterService<GameManager>(this) 이후 호출
    {
        StartCoroutine(StartFirstPhase());
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_requestPhaseChange);  
        EventBus.Subscribe(_onEndingConditionMet);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_requestPhaseChange);
        EventBus.Unsubscribe(_onEndingConditionMet);
    }
    public void ChangePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
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
                StartCoroutine(WaitAndChangePhase(GamePhase.Briefing, 1.5f)); // 1.5초 후 자동으로 브리핑페이즈로 전환
                break;
            case GamePhase.Briefing:
                OnEnterBriefing();
                StartCoroutine(WaitAndChangePhase(GamePhase.Patrol, 1.5f));
                break;
            case GamePhase.Patrol:
                OnEnterPatrol();
                break;
            case GamePhase.Settlement:
                OnEnterSettlement();
                StartCoroutine(WaitAndChangePhase(GamePhase.OffDuty, 1.5f));
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
        currentDay++;
    }
    private void OnEnterBriefing() // 브리핑 페이즈
    {

    }

    private void OnEnterPatrol() // 순찰 페이즈
    {
        StopAllCoroutines();
        StartCoroutine(UpdateTimer()); // 타이머 코루틴 시작
    }

    private void OnEnterSettlement() // 정산 페이즈
    {

    }

    private void OnEnterOffDuty() // 퇴근 페이즈
    {

    }

    private void OnEnterEnding() // 엔딩 페이즈
    {
        //추후 엔딩 연출 추가
        Debug.Log("엔딩 페이즈 진입");
        Debug.Log($"{finalEnding}에 진입하였습니다.");
        OnGameEnded?.Invoke(finalEnding);
    }

    private IEnumerator UpdateTimer() // 타이머 코루틴
    {
        yield return new WaitForSeconds(1.0f);
        while(CurrentPhase == GamePhase.Patrol && patrolDurationSeconds > 0)
        {
            patrolDurationSeconds -= Time.deltaTime;
            OnInGameTimeUpdated?.Invoke(patrolDurationSeconds);
            yield return null;
        }
        if (patrolDurationSeconds < 0)
        {
            patrolDurationSeconds = 0;
            ChangePhase(GamePhase.Settlement);
        }
    }
    private IEnumerator StartFirstPhase() // 다른 매니저들 초기화 기다림
    {
        yield return null;
        ChangePhase(GamePhase.Standby);
    }

    private IEnumerator WaitAndChangePhase(GamePhase nextPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangePhase(nextPhase);
    }
}
