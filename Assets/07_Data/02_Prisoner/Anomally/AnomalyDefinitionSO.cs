using UnityEngine;

public enum AnomalyKind
{
    BrickColor,
    BedLegThickness,
    PosterFlap,
    ToiletCleanSpot,
    ItemInspect
}

[CreateAssetMenu(menuName = "GameData/Anomaly Definition", fileName = "AnomalyDef")]
public class AnomalyDefinitionSO : ScriptableObject
{
    public string anomalyId;           // A_001 같은 식별자
    public AnomalyKind kind;

    [Header("Spawn")]
    public GameObject normalPrefab;    // 정상 표현
    public GameObject suspiciousPrefab; // 수상 표현

    [Header("Inspect Text (MVP)")]
    [TextArea] public string normalDesc;
    [TextArea] public string suspiciousDesc;
}
