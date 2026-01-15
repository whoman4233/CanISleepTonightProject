using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrankSpawnManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject seniorGuardPrefab; // 선임 교도관 프리팹
    [SerializeField] private Transform spawnPoint; // 선임 교도관 스폰 위치

    private GameObject _currentGuard;

    private void OnEnable() => EventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        // 스탠바이 페이즈 시 미션6 이면 소환
        if (e.Phase == GamePhase.Standby)
        {
            if (DailyMissionManager.Instance.CurrentMission is Mission06Strategy)
            {
                SpawnFrank();
            }
        }
        else if (e.Phase == GamePhase.NotStarted)
        {
            CleanupGuard();
        }
    }

    private void SpawnFrank()
    {
        if (_currentGuard != null) return;

        if (seniorGuardPrefab != null && spawnPoint != null)
        {
            _currentGuard = Instantiate(seniorGuardPrefab, spawnPoint.position, spawnPoint.rotation);
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
