using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("페이즈 상태")]
    [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
    public GamePhase CurrentPhase => currentPhase;
    [SerializeField] private int currentDay = 1;

    [SerializeField] private float initialRiotGauge = 30f; // 기본 폭동 게이지
    [SerializeField] private float dailyRiotIncrease = 20f; // 폭동게이지 하루 기본 증가량
    [SerializeField] private float maxRiotGauge = 100f; // max폭동게이지

    private float riotGauge; // 폭동게이지 혹시몰라서 float로 해 놓음.

    public event Action<GamePhase> OnPhaseChanged; // 페이즈 변경 이벤트
    public event Action<float> OnRiotGaugeChanged; // 폭동게이지 변경 이벤트
    public event Action OnGameOver; // 게임오버 이벤트

    public void Initialize()
    {
        riotGauge = initialRiotGauge;
        OnRiotGaugeChanged?.Invoke(riotGauge);
        Debug.Log($"게임매니저 초기화 완료. \n 현재 폭동수지{riotGauge}");
        
    }

    public void ChangePhase(GamePhase newPhase)
    {

    }
}
