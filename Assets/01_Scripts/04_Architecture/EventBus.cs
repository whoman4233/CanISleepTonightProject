using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    //구독자를 타입형태로 리스트로 관리
    private static readonly Dictionary<Type, IList> _subscribers = new();

    //IList에 리스트로 관리할 구독자들을 불러오기
    private static List<WeakReference<Action<T>>> GetList<T>()
    {
        var type = typeof(T);

        // 딕셔너리에 해당 이벤트 타입이 없다면 새 리스트를 생성하여 등록
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<WeakReference<Action<T>>>();
            _subscribers[type] = list;
        }

        //IList(콜백 저장된 리스트)를 실제 이벤트 형태의 타입으로 캐스팅해서 반환
        return (List<WeakReference<Action<T>>>)list;
    }

    //구독
    //(콜백을 약한 참조로 감싸서 해당 이벤트 타입의 구독 리스트에 추가)
    public static void Subscribe<T>(Action<T> callback)
    {
        GetList<T>().Add(new WeakReference<Action<T>>(callback));
    }

    //해지
    //이미 Destroy된 구독자 제거 및 전달된 콜백과 동일한 콜백 찾아 제거
    public static void Unsubscribe<T>(Action<T> callback)
    {
        var list = GetList<T>();

        list.RemoveAll(w =>
        {
            if (!w.TryGetTarget(out var target))
                return true;

            return target == callback;
        });
    }

    // [추가] 모든 구독 정보 초기화 (씬 전환 시 호출용)
    public static void Clear()
    {
        _subscribers.Clear();
        Debug.Log("[EventBus] 모든 이벤트 구독자가 초기화되었습니다.");
    }

    //발행
    public static void Publish<T>(T eventData)
    {
        var list = GetList<T>();

        var snapshot = list.ToArray(); // 리스트 변경 후 오류방어를 위한 복사본 생성

        foreach (var weak in snapshot)
        {
            // Destroy된 객체 Traget null -> 자동 제거 (신 변경)
            if (!weak.TryGetTarget(out var callback))
            {
                list.Remove(weak);
                continue;
            }

            // 예외처리(개별 구독자 오류로 전체 Publish가 중단되지 않도록 보호처리)
            try
            {
                callback.Invoke(eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventBus] {typeof(T).Name} 처리 중 예외: {ex}");
            }
        }
    }

    public static void ClearLocalEvents() // 튜토리얼 이벤트 초기화 (임시로 튜토리얼만 초기화함)
    {
        // 씬 전환 시 날려버려야 할 지역적 이벤트 타입들 정의
        List<Type> typesToRemove = new List<Type>
    {
        typeof(DialogueStepChangedEvent)
    };

        foreach (var type in typesToRemove)
        {
            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type].Clear();
                Debug.Log($"[EventBus] 지역 이벤트 초기화: {type.Name}");
            }
        }
    }
}