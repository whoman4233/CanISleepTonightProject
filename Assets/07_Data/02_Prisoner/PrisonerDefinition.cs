using UnityEngine;
using static PrisonerSpawnController;

[System.Serializable]
public class PrisonerDefinition
{
    public string templateId;
    public string displayName;

    public GameObject prisonerPrefab;

    [Header("Types")]
    public PrisonerAIType aiType;       // 기존 type -> aiType으로 명확히 변경 추천 (Good/Bad)
    public PrisonerType traitType;      // [추가] 이상현상 매칭용 특성 (Muscular, Nervous...)

    public int hp;
    public int atk;
    public int spd;

    public bool isQte;
    public string qteId;
    [TextArea] public string info; 
}