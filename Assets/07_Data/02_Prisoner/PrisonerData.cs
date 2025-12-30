[System.Serializable]
public class PrisonerData
{
    // 고유 정보
    public string ID;
    public string Name;
    public PrisonerDefinition Definition; // 원본 SO

    // [추가] 실제 런타임에 적용될 성향 (Definition.aiType 대신 이거 씀)
    public PrisonerAIType RuntimeAIType;

    // 런타임 상태
    public float CurrentHealth;
    public float MaxHealth;
    public bool IsSuppressed;

    // [변경] 생성자에서 aiTypeOverride를 받음
    public PrisonerData(PrisonerDefinition so, PrisonerAIType aiTypeOverride)
    {
        this.ID = System.Guid.NewGuid().ToString();
        this.Name = so.displayName;
        this.Definition = so;

        // 여기서 덮어쓰기!
        this.RuntimeAIType = aiTypeOverride;

        this.MaxHealth = so.hp;
        this.CurrentHealth = so.hp;
    }
}