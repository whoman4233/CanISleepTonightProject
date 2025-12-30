// [변경] PrisonCellManager.cs -> PrisonerManager.cs로 이름 변경 및 기능 통합
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PrisonerManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<CellAnchor> cells; // 기존 CellManager 기능 흡수

    // 활성화된 죄수 목록 (관리 용이)
    private List<PrisonerController> activePrisoners = new List<PrisonerController>();

    // 외부(GameManager 등)에서 호출하는 메서드
    public void SpawnPrisoner(PrisonerDefinition dataSO)
    {
        // 1. 빈 감옥 찾기
        CellAnchor emptyCell = GetEmptyCell();
        if (emptyCell == null)
        {
            Debug.LogWarning("빈 감옥이 없어 죄수를 스폰할 수 없습니다.");
            return;
        }

        // 2. 데이터 컨테이너 생성
        PrisonerData newData = new PrisonerData(dataSO);

        // 3. 프리팹 생성 및 초기화
        GameObject obj = Instantiate(prisonerPrefab, spawnPoints[0].position, Quaternion.identity);
        PrisonerController controller = obj.GetComponent<PrisonerController>();

        // 4. 데이터 주입 (Init 메서드 하나로 데이터 전달 해결)
        controller.Initialize(newData, emptyCell);

        // 5. 관리 리스트 추가
        activePrisoners.Add(controller);
        emptyCell.IsOccupied = true;
    }

    private CellAnchor GetEmptyCell()
    {
        return cells.FirstOrDefault(c => !c.IsOccupied);
    }

    // 정산 로직 등을 위해 죄수 데이터를 한꺼번에 반환
    public List<PrisonerData> GetAllPrisonerData()
    {
        return activePrisoners.Select(p => p.Data).ToList();
    }
}