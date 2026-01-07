[System.Serializable]
public class PrisonerData
{
    // 고유 정보
    public string ID;   // 죄수 고유 ID (예: GUID)
    public string CellID; // ★ [추가] 이 죄수가 배정된 방 번호 (예: "Cell_101")
    public string Name;

    public PrisonerDefinition definition;
    public PrisonerAIType RuntimeAIType;
    public DailyRoleData dailyRole;

    public float CurrentHealth;
    public float MaxHealth;
    public bool IsSuppressed;

    // [수정] 생성자에 string cellId 추가
    public PrisonerData(PrisonerDefinition so, PrisonerAIType aiTypeOverride, string cellId, string instanceId = "")
    {
        // 인스턴스 ID가 없으면 자동 생성, 있으면 그대로 사용
        this.ID = string.IsNullOrEmpty(instanceId) ? System.Guid.NewGuid().ToString() : instanceId;

        this.CellID = cellId; // ★ [추가] 방 번호 저장

        this.Name = so.displayName;
        this.definition = so;

        // 초기화 시 Role 데이터 기본값
        this.dailyRole = new DailyRoleData(false, aiTypeOverride, VisualAnomalyType.None);

        this.MaxHealth = so.hp;
        this.CurrentHealth = so.hp;
        this.IsSuppressed = false;
    }
}