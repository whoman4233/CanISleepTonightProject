using UnityEngine;

[CreateAssetMenu(menuName = "QTE/QTE Action")]
public class QTEActionSO : ScriptableObject
{
    [Header("ID")]
    public string qteId;

    [Header("QTE Rule")]
    public QTEType type;// Mash(연타) / Hold(지속)
    public float timeLimit; // QTE 제한시간
    public float requiredValue; // QTE 요구 입력량

    [Header("Mash Settings(연타)")]
    public float perPressValue; // 한번의 입력당 차는 양
    public float decayDelay; //입력과 입력사이의 지연값
    public float decayPerSecond; // 초당 입력량을 줄이는 값 (힘겨루기 느낌)

    [Header("Fail / Success Meaning(실패/성공 시)")]
    public int damageOnFail; //임시(데미지처리?)
}
