using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrankSpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPoints_B1; // 미션 4, 6 (지하)
    [SerializeField] private Transform spawnPoints_1F; // 나머지 (1층)

    [Header("Prefab")]
    [SerializeField] private GameObject frankPrefab;
    [SerializeField] private GameObject frankSitPrefab;

    private GameObject _currentFrankInstance;

    private Action<MissionStartedEvent> _onMissionStart;

    private void Awake()
    {
        _onMissionStart = OnMissionStarted;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onMissionStart); // 미션쪽에서 발행하는 이벤트 구독
        if (DailyMissionManager.Instance != null && DailyMissionManager.Instance.CurrentMission != null)
        {
            Debug.Log("[FrankSpawn] 이미 미션이 진행 중임을 감지, 즉시 스폰 시도.");
            SpawnFrank(DailyMissionManager.Instance.CurrentMission);
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onMissionStart);
    }


    private void OnMissionStarted(MissionStartedEvent e) // 이벤트 발행 후(미션 세팅) 프랭크 소환
    {
        SpawnFrank(e.mission);
    }

    public void SpawnFrank(DailyMissionStrategy mission)
    {
        // 기존 인스턴스 정리
        ClearFrank();
        if (_currentFrankInstance != null) return;
        if (mission == null) return;

        // 미션 4일 때는 소환 안 함
        if (mission.missionId == DialogueKeys.Missions.Mission04)
        {
            Debug.Log("[FrankSpawn] 미션 4단계: 프랭크 스폰 스킵");
            return;
        }

        GameObject prefabToSpawn = null;
        Transform targetPoint = null;

        // 미션 6인지 체크
        bool isMission06 = (mission is Mission06Strategy || mission.missionId == DialogueKeys.Missions.Mission06);

        if (isMission06)
        {
            // 미션 6: 지하(B1) + 서 있는 프랭크
            prefabToSpawn = frankPrefab;
            targetPoint = spawnPoints_B1;
        }
        else
        {
            // 나머지 미션: 1층(1F) + 앉아 있는 프랭크
            prefabToSpawn = frankSitPrefab;
            targetPoint = spawnPoints_1F;
        }

        // 최종 생성 절차
        if (prefabToSpawn != null && targetPoint != null)
        {
            _currentFrankInstance = Instantiate(prefabToSpawn, targetPoint.position, targetPoint.rotation);
            _currentFrankInstance.name = DialogueKeys.Speakers.Frank;

            Debug.Log($"[FrankSpawn] 미션 {mission.missionId} 설정: " +
                      $"프리팹({prefabToSpawn.name}), 위치({targetPoint.name}) 스폰 완료");
        }
        else
        {
            Debug.LogWarning($"[FrankSpawn] 스폰 실패: 프리팹({prefabToSpawn}) 또는 포인트({targetPoint})가 없습니다.");
        }
    }

    public void ClearFrank()
    {
        if (_currentFrankInstance != null)
        {
            Destroy(_currentFrankInstance);
            _currentFrankInstance = null;
        }
    }
}