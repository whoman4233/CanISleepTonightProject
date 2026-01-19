using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrankSpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints_B1; // 미션 4, 6 (지하)
    [SerializeField] private Transform[] spawnPoints_1F; // 나머지 (1층)

    [Header("Prefab")]
    [SerializeField] private GameObject frankPrefab;

    private GameObject _currentFrankInstance;

    // ★ [수정] 인자를 dayIndex가 아닌 missionID로 받도록 변경 (혹은 내부에서 확인)
    public void SpawnFrankForMission(DailyMissionStrategy mission)
    {
        ClearFrank();

        if (mission == null) return;

        // 미션 ID 파싱 (예: "Mission_04" -> 4)
        int missionNum = ParseMissionID(mission.missionId);

        Transform targetPoint = null;

        // ★ [핵심] 미션 번호에 따라 층 결정
        // 미션 4, 6은 B1층 / 나머지는 1F
        //if (missionNum == 4 || missionNum == 6)
        //{
        //    if (spawnPoints_B1 != null && spawnPoints_B1.Length > 0)
        //    {
        //        targetPoint = spawnPoints_B1[Random.Range(0, spawnPoints_B1.Length)];
        //    }
        //}
        if (missionNum == 4) return;
        else if (missionNum == 6) // 미션 6에서만 b1F에서 소환되도록
        {
            if (spawnPoints_B1 != null && spawnPoints_B1.Length > 0)
            {
                targetPoint = spawnPoints_B1[Random.Range(0, spawnPoints_B1.Length)];
            }
        }
        else
        {
            if (spawnPoints_1F != null && spawnPoints_1F.Length > 0)
            {
                targetPoint = spawnPoints_1F[Random.Range(0, spawnPoints_1F.Length)];
            }
        }

        if (targetPoint != null && frankPrefab != null)
        {
            _currentFrankInstance = Instantiate(frankPrefab, targetPoint.position, targetPoint.rotation);
            Debug.Log($"[Frank] 선임 교도관 생성 완료 (Mission {missionNum}, 위치: {targetPoint.name})");
        }
        else
        {
            Debug.LogWarning("[Frank] 스폰 포인트가 없거나 프리팹이 없습니다.");
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

    private int ParseMissionID(string missionID)
    {
        // "Mission_04" 같은 문자열에서 숫자 추출
        string numberPart = System.Text.RegularExpressions.Regex.Replace(missionID, @"\D", "");
        if (int.TryParse(numberPart, out int result))
        {
            return result;
        }
        return 1; // 실패 시 기본값
    }
}