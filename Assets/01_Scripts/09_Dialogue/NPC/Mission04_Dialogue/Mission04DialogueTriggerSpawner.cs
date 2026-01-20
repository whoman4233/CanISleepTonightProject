using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 동일 씬 내에서
/// Mission4 다이얼로그 트리거를
/// 생성 / 제거 관리하는 전용 Spawner
/// </summary>
public class Mission4DialogueTriggerSpawner : MonoBehaviour
{
    private Action<Mission4DialogueTriggerSpawnEvent> _onSpawn;

    // 현재 생성된 트리거들 추적
    private readonly List<GameObject> _spawnedTriggers = new();

    private void Awake()
    {
        _onSpawn = OnSpawnRequested;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onSpawn);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSpawn);
        ClearAllTriggers();
    }

    private void OnSpawnRequested(Mission4DialogueTriggerSpawnEvent e)
    {
        // ★ 안전장치: Mission4가 아닐 경우
        var mission =
            DailyMissionManager.Instance?.CurrentMission
            as Mission_FindImposterStrategy;

        if (mission == null)
        {
            // Mission4가 아니면 기존 트리거 제거
            ClearAllTriggers();
            return;
        }

        // 중복 생성 방지
        ClearAllTriggers();

        if (e.Triggers == null || e.Triggers.Count == 0)
            return;

        foreach (var def in e.Triggers)
        {
            if (def.triggerPrefab == null)
                continue;

            var go = Instantiate(
                def.triggerPrefab,
                def.spawnPosition,
                Quaternion.Euler(def.spawnRotation)
            );

            _spawnedTriggers.Add(go);
        }
    }

    private void ClearAllTriggers()
    {
        if (_spawnedTriggers.Count == 0)
            return;

        foreach (var go in _spawnedTriggers)
        {
            if (go != null)
                Destroy(go);
        }

        _spawnedTriggers.Clear();
    }
}
