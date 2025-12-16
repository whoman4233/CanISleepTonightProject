using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSGBootstrap : MonoBehaviour
{
    [Header("매니저 연결")]
    [SerializeField] GameManager gameManager;
    [SerializeField] PlayerManager playerManager;
    [SerializeField] PlayerController playerController;

    [Header("플레이어 연결")]
    [SerializeField] private Player player;

    private void Awake()
    {
        if(GameContext.Instance == null)
        {
            Debug.LogError("GameContext가 씬에 없음");
            return;
        }
        RegisterAllServices();
        InitializeAllServices();
        //gameManager.ChangePhase(GamePhase.Standby); // 스탠바이페이즈로 시작

        Debug.Log("BootStrap.게임시스템 초기화 완료");
    }

    private void RegisterAllServices() //서비스 등록
    {
        GameContext context = GameContext.Instance; // 나중에 추가 등록 시 불편하지 않기 위해 context로 함축
        context.RegisterService<PlayerController>(playerController);
        context.RegisterService<GameManager>(gameManager);
        context.RegisterService<PlayerManager>(playerManager);
    }

    private void InitializeAllServices() // 초기화 순서(의존성이 낮은 순서대로 초기화)
    {
       // playerController.Initialize();
        playerManager.Initialize();
        gameManager.Initialize();

        player.StartStateMachine();
        playerController.SetMovementEnabled(true);
    }
}
