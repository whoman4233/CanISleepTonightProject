using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrankSpawnManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject seniorGuardPrefab; // 선임 교도관 프리팹
    [SerializeField] private Transform spawnPointA; // 선임 교도관 스폰 위치
    [SerializeField] private Transform spawnPointB;

    private GameObject _currentGuard;

    private void OnEnable() => EventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        // 스탠바이 페이즈 시 미션6 이면 소환
        if (e.Phase == GamePhase.Standby)
        {
                SpawnFrank();
        }
        else if (e.Phase == GamePhase.NotStarted)
        {
            CleanupGuard();
        }
    }

    private void SpawnFrank()
    {
        if (_currentGuard != null) return;

        // 미션 4일 때는 프랭크를 소환하지 않고 즉시 종료
        var currentMission = DailyMissionManager.Instance.CurrentMission;
        if (currentMission != null && currentMission.missionId == DialogueKeys.Missions.Mission04)
        {
            Debug.Log("[FrankSpawn] 미션 4단계이므로 프랭크 스폰을 스킵합니다.");
            return;
        }

        if (seniorGuardPrefab != null && spawnPointA != null && spawnPointB != null)
        {
            // 미션 6이면 A, 그 외(4 제외)는 B
            bool isMission06 = (currentMission is Mission06Strategy);
            Transform targetPoint = isMission06 ? spawnPointA : spawnPointB;

            _currentGuard = Instantiate(seniorGuardPrefab, targetPoint.position, targetPoint.rotation);
            _currentGuard.name = DialogueKeys.Speakers.Frank;
        }
    }

    private void CleanupGuard()
    {
        if (_currentGuard != null)
        {
            Destroy(_currentGuard);
            _currentGuard = null;
        }
    }
}
