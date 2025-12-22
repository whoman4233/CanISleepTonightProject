//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class LSGBootstrap : MonoBehaviour
//{
//    private static bool _isInitialized = false;

//    private void Awake()
//    {
//        if (_isInitialized)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        _isInitialized = true;
//        DontDestroyOnLoad(gameObject);

//        Debug.Log("부트스트랩 초기화");

//        RegisterAllServices();
//        InitializeAllServices();
//    }

//    private void RegisterAllServices()
//    {
//        var context = GameContext.Instance;
//        context.RegisterService<SaveManager>(new SaveManager());
//        context.RegisterService<GameManager>(GetComponentInChildren<GameManager>());
//        context.RegisterService<PlayerManager>(GetComponentInChildren<PlayerManager>());
//        context.RegisterService<PrisonCellManager>(GetComponentInChildren<PrisonCellManager>());
//        context.RegisterService<SettlementManager>(GetComponentInChildren<SettlementManager>());
//        context.RegisterService<SettlementReportBuilder>(GetComponentInChildren<SettlementReportBuilder>());
//    }

//    private void InitializeAllServices()
//    {
//        GameContext.Instance.Get<GameManager>()?.Initialize();
//        GameContext.Instance.Get<PlayerManager>()?.Initialize();
//    }
//}
