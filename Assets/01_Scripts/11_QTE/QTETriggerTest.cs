using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QTETriggerTest : MonoBehaviour
{
    [Header("Test QTE Config(QTE설정)")]
    [SerializeField] private QTEType qteType = QTEType.Mash;
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private float requiredValue = 10f;

    [Header("Mash(연타)")]
    [SerializeField] private float perPressValue = 1f; //누를 때마다 차는 양
    [SerializeField] private float decayPerSecond = 3.5f; // 초당 감속되는 양
    [SerializeField] private float decayDelay = 0.2f; //입력 지연 지속시간(줄어들 수록 더 빠름)

    [Header("Hold(지속)")]
    [SerializeField] private float holdPerSecond = 3f;

    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        _triggered = true;

        var config = new QTEConfig
        {
            Type = qteType,
            TimeLimit = timeLimit,
            RequiredValue = requiredValue,
            PerPressValue = perPressValue,
            HoldPerSecond = holdPerSecond,
            DecayPerSecond = decayPerSecond,
            DecayDelay = decayDelay,

        };

        EventBus.Publish(new QTEStartedEvent
        {
            QTEId = "Test_QTE",
            Config = config
        });

        Debug.Log("[QTE TEST] QTEStartedEvent published");
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _triggered = false;
    }
}
