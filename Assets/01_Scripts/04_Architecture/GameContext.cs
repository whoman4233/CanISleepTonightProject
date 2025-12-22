//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class GameContext : MonoBehaviour
//{
//    //public static GameContext Instance { get; private set; }

//    private static GameContext _instance;
//    public static GameContext Instance
//    {
//        get
//        {
//            if (_instance == null)
//            {
//                _instance = FindObjectOfType<GameContext>();
//                if (_instance == null)
//                {
//                    GameObject contextObject = new GameObject("GameContext");
//                    _instance = contextObject.AddComponent<GameContext>();
//                    DontDestroyOnLoad(contextObject);
//                }
//            }
//            return _instance;
//        }
//    }

//    private Dictionary<Type, object> services = new Dictionary<Type, object>(); // 딕셔너리로 저장

//    //private void Awake()
//    //{
//    //    if(Instance != null && Instance != this)
//    //    {
//    //        Destroy(gameObject);
//    //        return;
//    //    }
//    //    Instance = this;
//    //    DontDestroyOnLoad(gameObject);
//    //} 

//    public void RegisterService<T>(T service) where T : class //서비스 등록관련 // T는 참조 타입(클래스)이어야 함을 명시
//    {
//        Type type = typeof(T); // T타입 정보를 가져옴 (예: typeof(PlayerManager))
//        if (services.ContainsKey(type)) // 이미 해당 타입의 서비스가 딕셔너리에 저장되어 있는지 확인
//        {
//            Debug.LogWarning($"{type.Name} is already registered."); // 이미 있으면 디버그로그 띄워주고 
//            return; // 등록안하기
//        }
//        services.Add(type, service); // if문 통과했으면 추가해줌
//        Debug.Log($"{type.Name} 등록 성공"); //GameContext context = GameContext.Instance; context.RegisterService<PlayerManager>(playerManager); 으로 서비스 등록 //Key: typeof(PlayerManager), Value: playerManager 인스턴스
//    } 

//    public T Get<T>() where T : class //GameContext.Instance.Get<PlayerManager>() 와 같은 방식으로 등록된 서비스를 참조
//    {
//        Type type = typeof(T);
//        if (services.TryGetValue(type, out object service))
//        {
//            return service as T; // 딕셔너리에서 찾은 object를 요청한 타입 T로 캐스팅하여 반환
//        }
//        Debug.LogError($"{type.Name}을 발견하지 못하였습니다.");
//        return null;
//    }

//}
