using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    //private Player Player;
    private GameManager gameManager;
    private PrisonCellManager prisonCellManager;

    public string CurrentCellID { get; private set; } = string.Empty; // ID = 감방 id, 플레이어가 어느 감방에 있는지, 감방id에 따라 int나 string로 변경
    public bool IsObserving { get; private set; } = false;      // 관찰 중 상태
    public bool IsEngagingInAction { get; private set; } = false; // 진압 액션 중

    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private void Awake()
    {
        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                ResetDailyRecoed();
            }
        };
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    public void Initialize()
    {
        //GameContext context = GameContext.Instance;
        ////Player = context.Get<Player>();
        //gameManager = context.Get<GameManager>();
        //prisonCellManager = context.Get<PrisonCellManager>();
        

        //if(gameManager == null || prisonCellManager == null)
        //{
        //    Debug.LogError("Player 혹은 GameManager가 연결되지 않았습니다");
        //    return;
        //}
       // SetMovementState(false);
    }

    //public void SetMovementState(bool state) // 플레이어 이동 가능 상태
    //{
    //    Player.SetMovementEnabled(state);
    //}

    public void ResetDailyRecoed() // 플레이어 상태 초기화
    {
        CurrentCellID = string.Empty;
        IsObserving = false;
        IsEngagingInAction = false;
        Debug.Log("플레이어 순찰 상태 초기화 완료");
    }
    public void SetInspectingCell(string cellID) => CurrentCellID = cellID;
    public void SetObserving(bool state) => IsObserving = state;
    public string GetCurrentCellID() => CurrentCellID;

}
