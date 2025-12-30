using UnityEngine;

public class AnomalyActor : MonoBehaviour
{
    public string cellId { get; private set; }
    public string anomalyId { get; private set; }
    public bool isSuspiciousVariant { get; private set; }

    private string _inspectText;

    public void Init(string cellId, AnomalyDefinitionSO def, bool suspiciousVariant)
    {
        this.cellId = cellId;
        anomalyId = def.anomalyId;
        isSuspiciousVariant = suspiciousVariant;

        _inspectText = suspiciousVariant ? def.suspiciousDesc : def.normalDesc;
    }

    // MVP: 클릭하면 로그로 검사 결과 표시 (UI 붙일 때 교체)
    private void OnMouseDown()
    {
        Debug.Log($"[Inspect] cell={cellId} anomaly={anomalyId} variant={(isSuspiciousVariant ? "SUSP" : "NORMAL")} msg={_inspectText}");
    }
}
    