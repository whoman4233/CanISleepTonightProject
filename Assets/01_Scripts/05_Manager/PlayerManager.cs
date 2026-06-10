using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] public GameObject playerPrefab; // 플레이어 프리팹
    [SerializeField] public Transform spawnPoint;    // 생성 위치

    protected GameObject currentPlayer;
    private Action<GamePhaseChangedEvent> _phaseChangedHandler;

    private void Awake()
    {
        _phaseChangedHandler = (e) => OnPhaseChanged(e);
    }

    //private void Start()
    //{
    //    if(GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.NotStarted)
    //    {
    //        SpawnPlayer();
    //    }
    //}

    private void OnEnable()
    {
        EventBus.Subscribe(_phaseChangedHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_phaseChangedHandler);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        // 스탠바이 페이즈로 진입할 때 플레이어 생성
        if (e.Phase == GamePhase.Standby || e.Phase == GamePhase.Settlement || e.Phase == GamePhase.Tutorial)
        {
            SpawnPlayer();
        }
        // 타이틀로 돌아갈 때(NotStarted) 기존 플레이어 삭제
        else if (e.Phase == GamePhase.NotStarted)
        {
            CleanupPlayer();
        }
    }

    public virtual void SpawnPlayer()
    {
        // 이미 플레이어가 있다면 중복 생성 방지
        if (currentPlayer != null) return;

        if (playerPrefab != null && spawnPoint != null)
        {
            currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("플레이어가 생성되었습니다.");
        }
        else
        {
            Debug.LogError("프리팹이나 스폰 포인트가 설정되지 않았습니다.");
        }
    }

    private void CleanupPlayer()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }
    }
}
