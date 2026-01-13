using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Mission06_Strategy", menuName = "Mission/Strategy/M06_Interrogation")]
public class Mission06Strategy : DailyMissionStrategy
{
    [Header("M06: Suspect Data")]
    [SerializeField] private Mission06Data missionData; // 랜덤 이름 저장용 SO
    private readonly string[] _originNames = { "Antony", "Richard", "Leo" };

    [Header("Spawn Rules")]
    public int targetSuspiciousCount;
    public int missionCountDown;
    public PrisonerAIType defaultAI = PrisonerAIType.Good;
    public List<PrisonerAIType> specialAIList;
    public List<VisualAnomalyType> specialVisualList;

    private bool _isCulpritCaught = false;

    private void OnValidate()
    {
        missionId = DialogueKeys.Missions.Mission06; // 미션id 고정
    }

    // 하루 시작 시 세팅 (DailyMissionManager가 호출)
    public override void SetupDay(AnomalyDistributor anomalyDistributor, PrisonerScheduleManager scheduleManager)
    {
        base.SetupDay(anomalyDistributor, scheduleManager);
        Debug.Log("<color=red>★★★ SetupDay 시작됨! ★★★</color>");

        _isCulpritCaught = false;
        AssignRandomNames(); // 이름 섞기
        specialAIList.Clear();
        specialVisualList.Clear();

        // 용의자 3명의 비주얼 타입
        specialVisualList.Add(VisualAnomalyType.Suspect1);
        specialVisualList.Add(VisualAnomalyType.Suspect2);
        specialVisualList.Add(VisualAnomalyType.Suspect3);

        // 용의자 3명의 AI 타입 (모두 Good)
        for (int i = 0; i < 3; i++) specialAIList.Add(PrisonerAIType.Good);

        // 설정된 AI와 Visual 목록을 스케줄 매니저에게 전달
        scheduleManager.AssignRolesForNewDay(
            suspiciousCount: targetSuspiciousCount,
            defaultAI: defaultAI,
            specialBehaviors: specialAIList,
            specialVisuals: specialVisualList
        );
        var spawnController = GameObject.FindObjectOfType<PrisonerSpawnController>();
        spawnController.ClearAllForNewDay(); // 이전 생성물 제거
        spawnController.SpawnAllPrisoners(); // 3명이 포함된 최신 데이터로 재생성
        Debug.Log("오늘의찐막테스트");
    }

    private void AssignRandomNames()
    {
        // 이름 셔플 후 Mission06Data에 저장
        var shuffled = _originNames.OrderBy(x => System.Guid.NewGuid()).ToList();
        missionData.Setup(shuffled[0], shuffled[1], shuffled[2]);

        Debug.Log($"범인 이름 세팅 완료. 범인이름은 {missionData.Suspect1Name}, 용의자2는 {missionData.Suspect2Name}, 용의자3은{missionData.Suspect3Name}");
    }

    // 이벤트 처리 (UI 버튼 등에서 호출)
    public override void OnEventTriggered(string eventCode)
    {
        if (eventCode == "M06_Success")
        {
            _isCulpritCaught = true;
        }
    }

    // 승리 조건 판정 (결산 시 호출)
    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        if (_isCulpritCaught)
        {
            failReason = "";
            return true;
        }

        failReason = "진범을 지목하지 못했습니다.";
        return false;
    }

    // DialogueManager에서 사용할 텍스트 가공 인터페이스
    public override string GetProcessedText(string rawText)
    {
        if (missionData == null) return rawText;
        return missionData.ProcessText(rawText); // Suspect1를 랜덤배치된 이름으로 치환
    }

    // 매개변수로 PrisonerController를 받지만, 내부에서는 CellId를 통해 매니저를 조회합니다.
    public override bool IsValidPrisoner(PrisonerController prisoner)
    {
        // 1. 죄수의 방 ID(CellId) 가져오기
        // (PrisonerController에 AssignedCell이 있고, 그 안에 CellId가 있다고 가정)
        if (prisoner == null || prisoner.AssignedCell == null) return false;

        string cellId = prisoner.AssignedCell.cellId;

        // 2. 스케줄 매니저에게 "오늘 이 방 죄수의 역할 정보" 요청
        if (PrisonerScheduleManager.Instance == null) return false;

        // ★ 여기서 핵심: DailyRoleData 구조체를 가져옵니다.
        DailyRoleData roleData = PrisonerScheduleManager.Instance.GetDailyRole(cellId);

        // 3. AI 타입 조건 확인 (리스트에 포함되어 있는지)
        if (specialAIList.Contains(roleData.dailyAIType))
        {
            return true;
        }

        // 4. Visual 타입 조건 확인 (리스트에 포함되어 있는지)
        // DailyRoleData에 visualType이 있으므로 아주 깔끔하게 비교 가능합니다.
        if (specialVisualList.Contains(roleData.visualType))
        {
            return true;
        }

        return false;
    }
}
