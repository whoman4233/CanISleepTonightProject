using System;

[Flags]
public enum MissionDayTheme
{
    None = 0,
    NoiseEvent = 1 << 0,  // 1일차: 소음
    FindWeapon = 1 << 1,  // 2일차: 흉기 찾기
    VisualAnomaly = 1 << 2,  // 3일차: 비주얼 이상
    FindImposter = 1 << 3,  // 4일차: 변장/실종
    ConfiscateItem = 1 << 4,  // 5일차: 금지물품
    Interrogation = 1 << 5,  // 6일차: 추리
    BossRiot = 1 << 6,  // 7일차: 폭동

    // 유틸리티: 수색 미션 공통 (흉기 + 금지물품)
    SearchMission = FindWeapon | ConfiscateItem,
    All = ~0
}