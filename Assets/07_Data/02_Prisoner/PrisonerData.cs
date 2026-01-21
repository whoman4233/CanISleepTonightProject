[System.Serializable]
public class PrisonerData
{
    // 고유 정보
    public string ID;    // 죄수 고유 ID (예: GUID)
    public string CellID; // 이 죄수가 배정된 방 번호
    public string Name;

    public PrisonerDefinition definition;
    public PrisonerAIType RuntimeAIType;
    public DailyRoleData dailyRole;

    // ★ [핵심 수정] 런타임 스탯 변수 (체력, 공격력)
    public float CurrentHealth;
    public float MaxHealth;
    public float AttackPower; // ★ 공격력 추가 (이게 없으면 플레이어를 때려도 데미지가 0)

    public bool IsSuppressed;

    // 생성자
    public PrisonerData(PrisonerDefinition so, PrisonerAIType aiTypeOverride, string cellId, string instanceId = "")
    {
        // 1. ID 설정 (인스턴스 ID가 없으면 자동 생성)
        this.ID = string.IsNullOrEmpty(instanceId) ? System.Guid.NewGuid().ToString() : instanceId;
        this.CellID = cellId;

        // 2. 기본 정보 설정
        this.Name = so.displayName;
        this.definition = so;

        // 3. 역할 및 AI 설정
        // 초기화 시 Role 데이터 기본값
        this.dailyRole = new DailyRoleData(false, aiTypeOverride, VisualAnomalyType.None);
        this.RuntimeAIType = aiTypeOverride; // RuntimeAI도 초기화 필요

        // ====================================================================
        // ★ [문제 해결] 스탯 초기화 안전장치 (데이터 누락 방지)
        // ====================================================================

        // 4. 체력 설정 (SO 설정값이 0이면 기본값 100 부여 -> 급사 방지)
        this.MaxHealth = so.hp > 0 ? so.hp : 100f;
        this.CurrentHealth = this.MaxHealth;

        // 5. 공격력 설정 (기본값 10 부여 -> 노딜 방지)
        // 만약 PrisonerDefinition(so)에 'damage'나 'attackPower' 변수가 있다면 그걸 사용하세요.
        // 예: this.AttackPower = so.baseDamage > 0 ? so.baseDamage : 10f;
        this.AttackPower = 10f;

        this.IsSuppressed = false;
    }

    // ★ [필수 추가] 매일 아침 매니저가 호출할 초기화 함수
    public void ResetDailyFlags()
    {
        this.IsSuppressed = false;
        this.CurrentHealth = this.MaxHealth; 
    }
}