using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필수

[CreateAssetMenu(menuName = "Missions/Type: Collection (Day 2, 5)")]
public class Mission_CollectionStrategy : DailyMissionStrategy
{
    [Header("Collection Settings")]
    public string targetItemTag; // 예: "Weapon", "Contraband"

    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        base.SetupDay(ad, sm);

        // 1. [기존] 먼저 용의자(Suspicious)들을 배정합니다. (이때 아이템 소지 여부가 결정됨)
        sm.AssignRolesForNewDay(suspiciousCount: targetScore, defaultAI: PrisonerAIType.Good);

        // ========================================================================
        // 2. [수정] 미션 2번일 때, '용의자' 중 한 명을 골라 QTE 공격수로 변경
        // ========================================================================
        if (missionId == "Mission02")
        {
            List<string> allCells = sm.GetActiveCellIds();
            List<string> suspiciousCells = new List<string>();

            // (1) 용의자로 설정된 방만 골라내기
            foreach (var cellId in allCells)
            {
                // GetDailyRole로 역할 확인
                DailyRoleData role = sm.GetDailyRole(cellId);
                if (role.isSuspicious)
                {
                    suspiciousCells.Add(cellId);
                }
            }

            // (2) 용의자가 한 명이라도 있다면 그중 랜덤 선택
            if (suspiciousCells.Count > 0)
            {
                string targetCell = suspiciousCells[Random.Range(0, suspiciousCells.Count)];
                DailyRoleData currentRole = sm.GetDailyRole(targetCell);

                // (3) 해당 용의자의 AI만 'QTE_Attacker'로 변경 (나머지 상태 유지)
                sm.SetDailyRole(
                    targetCell,
                    PrisonerAIType.QTE_Attacker, // ★ AI 교체: 공격 모드
                    currentRole.visualType,      // 외형 유지
                    true                         // ★ 용의자 상태(Suspicious) 유지 (중요!)
                );

                Debug.Log($"[Mission_CollectionStrategy] Mission02 이벤트: 용의자인 {targetCell}번 방 죄수가 QTE 공격수로 지정되었습니다.");
            }
            else
            {
                Debug.LogWarning("[Mission] 용의자가 한 명도 없어 QTE 공격수를 배정하지 못했습니다.");
            }
        }
    }

    // 아이템을 찾았을 때(클릭했을 때) 호출됨
    public override void OnEventTriggered(string eventCode)
    {
        if (eventCode.Contains(targetItemTag))
        {
            Debug.Log($"[Mission] 목표 아이템 발견! ({eventCode})");
        }
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        if (currentScore >= targetScore)
        {
            failReason = "";
            return true;
        }
        failReason = $"목표 물품을 {targetScore}개 찾아야 합니다. (현재: {currentScore})";
        return false;
    }

    // 아이템 검증 오버라이드
    public override bool IsValidItem(string itemTag)
    {
        return itemTag.Contains(targetItemTag);
    }
}