using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission06_Strategy", menuName = "Mission/Strategy/M06_Interrogation")]
public class Mission06Strategy : DailyMissionStrategy
{
    [Header("M06: Suspect Data")]
    [SerializeField] private Mission06Data missionData; // 랜덤 이름 저장용 SO
    public Mission06Data MissionData => missionData;
    private readonly string[] _originNames = { "Antony", "Richard", "Leo" };

    [Header("Spawn Rules")]
    public int targetSuspiciousCount;
    public int missionCountDown;
    public PrisonerAIType defaultAI = PrisonerAIType.Good;
    public List<PrisonerAIType> specialAIList;
    public List<VisualAnomalyType> specialVisualList;

    private bool _isCulpritCaught = false;
    [Header("Choices")]
    private int _current = 0;
    private int _target = 1; // 범인 지목 1회가 목표
    private bool _hasReported = false;
    public int CurrentCount => _current;
    public int TargetCount => _target;
    public bool HasReported => _hasReported;

    private Action<Mission06SuspectSelectedEvent> _onSuspectSelected;
    private void OnEnable()
    {
        _hasReported = false;
        _current = 0;
        _isCulpritCaught = false;

        _onSuspectSelected = OnSuspectSelected;
        EventBus.Subscribe(_onSuspectSelected);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSuspectSelected);
    }

    private void OnSuspectSelected(Mission06SuspectSelectedEvent e)
    {
        SubmitReport(e.selectedIndex);
    }
    private void OnValidate()
    {
        missionId = DialogueKeys.Missions.Mission06; // 미션id 고정
    }

    // 하루 시작 시 세팅 (DailyMissionManager가 호출)
    public override void SetupDay(AnomalyDistributor anomalyDistributor, PrisonerScheduleManager scheduleManager)
    {
        base.SetupDay(anomalyDistributor, scheduleManager);
        Debug.Log("<color=red>★★★ SetupDay 시작됨! ★★★</color>");

        // =========================
        // Mission06 Runtime Reset
        // =========================
        _hasReported = false;
        _current = 0;
        _isCulpritCaught = false;
        AssignRandomNames(); // 이름 섞기
        List<string> allCellIds = scheduleManager.GetActiveCellIds();

        // 무작위로 3개의 방을 선정 (용의자가 될 방)
        // 리스트를 복사해서 셔플한 뒤 3개를 뽑음
        List<string> targetCells = allCellIds.OrderBy(x => UnityEngine.Random.value).Take(3).ToList();

        // 선정된 3명의 데이터를 "갱단원"으로 강제 치환
        // targetTemplateId는 SO에 등록된 갱단원의 templateId와 일치시켜야함
        foreach (var cellId in targetCells)
        {
            scheduleManager.ForceTransformPrisoner(cellId, "PSN_Gang_01");
        }

        // 역할 부여 (중앙 소환을 위한 비주얼 타입 지정)
        // 첫 번째 방(targetCells[0])을 진범(isSuspicious = true)으로 설정
        scheduleManager.SetDailyRole(targetCells[0], PrisonerAIType.Good, VisualAnomalyType.Suspect1, true);
        scheduleManager.SetDailyRole(targetCells[1], PrisonerAIType.Good, VisualAnomalyType.Suspect2, false);
        scheduleManager.SetDailyRole(targetCells[2], PrisonerAIType.Good, VisualAnomalyType.Suspect3, false);

        // 나머지 방들은 평범한 죄수들로 채우기 (이미 정해진 3명 제외)
        foreach (var cellId in allCellIds)
        {
            if (!targetCells.Contains(cellId))
            {
                scheduleManager.SetDailyRole(cellId, defaultAI, VisualAnomalyType.None, false);
            }
        }

        // 스폰 실행
        var spawnController = GameObject.FindObjectOfType<PrisonerSpawnController>();
        if (spawnController != null)
        {
            spawnController.ClearAllForNewDay();
            spawnController.SpawnAllPrisoners();
        }

        var allPrisoners = GameObject.FindObjectsOfType<PrisonerController>();
        foreach (var prisoner in allPrisoners) // 미션 6 셋업 시 죄수 체력 말도안되게 올려서 안죽게 만들기. 꼬우면 다 깎아보셈~
        {
            if (prisoner.Data != null)
            {
                prisoner.Data.MaxHealth = 9999f;
                prisoner.Data.CurrentHealth = 9999f;
            }
        }
        Debug.Log("갱단원3명 소환");
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

    public override bool IsValidPrisoner(string cellId)
    {
        // 1. 스케줄 매니저 확인
        if (PrisonerScheduleManager.Instance == null) return false;

        DailyRoleData role = PrisonerScheduleManager.Instance.GetDailyRole(cellId);

        // 3. AI 조건 확인 (구조체 안의 변수 사용)
        if (specialAIList.Contains(role.dailyAIType))
            return true;

        // 4. Visual 조건 확인 (구조체 안의 변수 사용)
        if (specialVisualList.Contains(role.visualType))
            return true;

        return false;
    }

    // 선임 교도관 NPC가 선택지를 클릭했을 때 호출할 함수
    public void SubmitReport(int choiceIndex)
    {
        // 이미 보고했다면 무시
        if (_hasReported)
        {
            Debug.LogWarning("[Mission06] 이미 보고가 완료되었습니다.");
            return;
        }

        _hasReported = true;

        // 지목 시점에 무조건 1회 카운트
        _current = 1;

        // 정답 여부 판정
        if (choiceIndex == 0)
        {
            _isCulpritCaught = true;
            Debug.Log("진범(용의자 1)을 지목했습니다.");
        }
        else
        {
            _isCulpritCaught = false;
            Debug.Log($"{choiceIndex + 1}번 용의자를 지목하여 실패했습니다.");
        }

        // HUD 갱신 알림 (프로젝트에 맞는 이벤트 사용)
        EventBus.Publish(new MissionProgressChangedEvent
        {
            current = _current,
            target = _target
        });
    }

}
