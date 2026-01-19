using UnityEngine;

public enum AnomalyCategory
{
    Common,     // 공통
    Individual, // 개별 (특정 죄수 타입 전용)
    Special     // 특수
}

public enum AnomalyKind
{
    Floor, FrontWall, LeftWall, RightWall, Poster, Tile, Vent,
    Toilet, Sink, Bed, Book, Trash,
    pot, weightDisc, dumbel, Drink, Lighter, SmutRag, Trump, Shoes, Planter, SmartPhone, Bread,
    ItemInspect, GeneralProp
}

[CreateAssetMenu(menuName = "GameData/Anomaly Definition", fileName = "AnomalyDef")]
public class AnomalyDefinitionSO : ScriptableObject
{
    public string anomalyId;
    public AnomalyKind kind;

    [Header("Spawn Settings")]
    public AnomalyTargetType targetType = AnomalyTargetType.Slot;

    [Header("Category Settings")]
    public AnomalyCategory category;
    public PrisonerType targetPrisoner = PrisonerType.None; // 개별일 때만 사용
    public int minRiotGauge = 0;

    [Header("Assets")]
    public GameObject normalPrefab;
    public GameObject suspiciousPrefab;

    [Header("Inspect Text")]
    [TextArea] public string normalDesc;
    [TextArea] public string suspiciousDesc;

    [Header("Behavior Flags")]
    [Tooltip("기존: 체크하면 이상현상이 아닐 때도 NormalPrefab 생성")]
    public bool alwaysSpawnNormal = false;

    // ★ [추가됨] 장식용 플래그
    [Tooltip("장식용: 체크하면 범인이 아닐 때 무조건 NormalPrefab 생성 (죄수 타입 일치 시)")]
    public bool isDecorative = false;

    [Header("Spawn Settings")]
    public MissionDayTheme validThemes;
}