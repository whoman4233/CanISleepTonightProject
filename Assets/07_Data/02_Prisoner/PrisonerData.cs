[System.Serializable]
public class PrisonerData
{
    // 고유 정보
    public string ID;   // SpawnController에서 만든 ID ("Cell_01_Target_01")
    public string Name;

    // [중요] 외부 코드(PrisonerController 등)와 대소문자 일치 확인 필요!
    // 기존 코드들이 .definition (소문자)를 쓴다면 아래처럼 바꾸세요.
    public PrisonerDefinition definition;

    // 실제 런타임에 적용될 성향
    public PrisonerAIType RuntimeAIType; 
    
    public DailyRoleData dailyRole;

    // 런타임 상태
    public float CurrentHealth;
    public float MaxHealth;
    public bool IsSuppressed;

    // [변경] 생성자 인자에 'string instanceId' 추가
    public PrisonerData(PrisonerDefinition so, PrisonerAIType aiTypeOverride, string instanceId = "")
    {
        this.ID = string.IsNullOrEmpty(instanceId) ? System.Guid.NewGuid().ToString() : instanceId;
        this.Name = so.displayName;
        this.definition = so;

        // 초기화 시 Role 데이터도 기본값으로 생성
        this.dailyRole = new DailyRoleData(false, aiTypeOverride, VisualAnomalyType.None);

        this.MaxHealth = so.hp;
        this.CurrentHealth = so.hp;
        this.IsSuppressed = false;
    }
}