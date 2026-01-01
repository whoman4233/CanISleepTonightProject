using UnityEngine;

public enum AnomalyCategory
{
    Common,     // 공통 (누구나, 언제나 등장 가능)
    Individual, // 개별 (특정 죄수 타입 전용)
    Special     // 특수 (폭동 게이지 조건)
}

// (기존 AnomalyKind는 슬롯 매칭용으로 유지)
public enum AnomalyKind
{
    Floor, FrontWall, LeftWall, RightWall, SteelBarred, Poster, Tile, Vent,
    Toilet, Sink, Bed, Book, Trash,
    pot, weightDisc, dumbel,
    ItemInspect, GeneralProp, // ... 기타 등등
                              
}

[CreateAssetMenu(menuName = "GameData/Anomaly Definition", fileName = "AnomalyDef")]
public class AnomalyDefinitionSO : ScriptableObject
{
    public string anomalyId;
    public AnomalyKind kind; // 어디에 스폰될지(슬롯 타입)

    [Header("Spawn Settings")]
    [Tooltip("Slot: 빈 곳에 생성, Bed/Toilet...: 기존 가구 교체")]
    public AnomalyTargetType targetType = AnomalyTargetType.Slot;

    [Header("Category Settings")]
    public AnomalyCategory category;

    // 개별(Individual)일 경우 대상 죄수 타입 (없으면 None)
    public PrisonerType targetPrisoner = PrisonerType.None;

    // 특수(Special)일 경우 필요한 최소 폭동 게이지
    public int minRiotGauge = 0;

    [Header("Assets")]
    public GameObject normalPrefab;      // 정상 상태 프리팹 (필수)
    public GameObject suspiciousPrefab;  // 이상현상 프리팹 (필수)

    [Header("Inspect Text")]
    [TextArea] public string normalDesc;
    [TextArea] public string suspiciousDesc;

    [Tooltip("체크하면 이상현상이 아닐 때도 NormalPrefab을 생성합니다. (예: 벽시계, 달력)")]
    public bool alwaysSpawnNormal = false;
}