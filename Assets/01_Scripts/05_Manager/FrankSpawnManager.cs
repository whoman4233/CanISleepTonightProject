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

    private GameObject _currentFrankInstance;

    private Action<MissionStartedEvent> _onMissionStart;

    // ★ [수정] 인자를 dayIndex가 아닌 missionID로 받도록 변경 (혹은 내부에서 확인)
    //public void SpawnFrankForMission(DailyMissionStrategy mission)
    //{
    //    ClearFrank();

    //    if (mission == null) return;

    //    // 미션 ID 파싱 (예: "Mission_04" -> 4)
    //    int missionNum = ParseMissionID(mission.missionId);

    //    Transform targetPoint = null;

    //    // ★ [핵심] 미션 번호에 따라 층 결정
    //    // 미션 4, 6은 B1층 / 나머지는 1F
    //    //if (missionNum == 4 || missionNum == 6)
    //    //{
    //    //    if (spawnPoints_B1 != null && spawnPoints_B1.Length > 0)
    //    //    {
    //    //        targetPoint = spawnPoints_B1[Random.Range(0, spawnPoints_B1.Length)];
    //    //    }
    //    //}
    //    if (missionNum == 4) return;
    //    else if (missionNum == 6) // 미션 6에서만 b1F에서 소환되도록
    //    {
    //        if (spawnPoints_B1 != null && spawnPoints_B1.Length > 0)
    //        {
    //            targetPoint = spawnPoints_B1[Random.Range(0, spawnPoints_B1.Length)];
    //        }
    //    }
    //    else
    //    {
    //        if (spawnPoints_1F != null && spawnPoints_1F.Length > 0)
    //        {
    //            targetPoint = spawnPoints_1F[Random.Range(0, spawnPoints_1F.Length)];
    //        }
    //    }

    //    if (targetPoint != null && frankPrefab != null)
    //    {
    //        _currentFrankInstance = Instantiate(frankPrefab, targetPoint.position, targetPoint.rotation);
    //        Debug.Log($"[Frank] 선임 교도관 생성 완료 (Mission {missionNum}, 위치: {targetPoint.name})");
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[Frank] 스폰 포인트가 없거나 프리팹이 없습니다.");
    //    }
    //}

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

        if (frankPrefab != null && spawnPoints_B1 != null && spawnPoints_1F != null)
        {
            // 미션 6이면 지하, 아니면 1층
            // (DailyMissionManager에서 전달받은 미션 객체로 판단)
            bool isMission06 = (mission is Mission06Strategy);
            Transform targetPoint = isMission06 ? spawnPoints_B1 : spawnPoints_1F;

            _currentFrankInstance = Instantiate(frankPrefab, targetPoint.position, targetPoint.rotation);
            _currentFrankInstance.name = DialogueKeys.Speakers.Frank;

            Debug.Log($"[FrankSpawn] 미션 {mission.missionId} 설정에 따라 {targetPoint.name}에 스폰 완료");
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

    //private int ParseMissionID(string missionID)
    //{
    //    // "Mission_04" 같은 문자열에서 숫자 추출
    //    string numberPart = System.Text.RegularExpressions.Regex.Replace(missionID, @"\D", "");
    //    if (int.TryParse(numberPart, out int result))
    //    {
    //        return result;
    //    }
    //    return 1; // 실패 시 기본값
    //}
}