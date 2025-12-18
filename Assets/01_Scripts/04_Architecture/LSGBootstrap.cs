using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSGBootstrap : MonoBehaviour
{
    [Header("매니저 연결")]
    [SerializeField] GameManager gameManager;
    [SerializeField] PlayerManager playerManager;
    //[SerializeField] PlayerController playerController;
    [SerializeField] PrisonCellManager prisonCellManager;
    [SerializeField] SettlementManager settlementManager;
    [SerializeField] SettlementReportBuilder settlementReportBuilder;


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
    private void Start()
    {
        gameManager.ChangePhase(GamePhase.Standby); // 스탠바이페이즈로 시작
    }

    private void RegisterAllServices() //서비스 등록
    {
        GameContext context = GameContext.Instance; // 나중에 추가 등록 시 불편하지 않기 위해 context로 함축
        //context.RegisterService<PlayerController>(playerController);
        SaveManager saveManager = new SaveManager(); // 세이브매니저는 MonoBehaviour가 아니기 때문에 여기서 생성 후 바로 등록
        context.RegisterService<SaveManager>(saveManager);
        context.RegisterService<SettlementReportBuilder>(settlementReportBuilder);
        context.RegisterService<GameManager>(gameManager);
        context.RegisterService<PlayerManager>(playerManager);
        context.RegisterService<PrisonCellManager>(prisonCellManager);
        context.RegisterService<SettlementManager>(settlementManager);
    }

    private void InitializeAllServices() // 초기화 순서(의존성이 낮은 순서대로 초기화)
    {
        //playerController.Initialize();
        gameManager.Initialize();
        playerManager.Initialize();
    }
}
