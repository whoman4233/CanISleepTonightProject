// [신규 파일] PrisonerData.cs
// MonoBehaviour가 아닌 일반 클래스로, 죄수의 모든 상태 값을 여기서 관리
[System.Serializable]
public class PrisonerData
{
    // 고유 정보
    public string ID;
    public string Name;
    public PrisonerDefinition Definition; // 원본 SO 참조

    // 런타임 상태 (변하는 값들)
    public float CurrentHealth;
    public float MaxHealth;
    public bool IsSuppressed; // 제압당함 여부

    // 생성자 (SO 데이터를 기반으로 초기화)
    public PrisonerData(PrisonerDefinition so)
    {
        this.ID = System.Guid.NewGuid().ToString();
        this.Name = so.displayName;
        this.Definition = so;
        this.MaxHealth = so.hp;
        this.CurrentHealth = so.hp;
    }
}