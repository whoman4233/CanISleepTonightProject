using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QTETriggerTest : MonoBehaviour
{
    [Header("Test QTE Config")]
    [SerializeField] private QTEType qteType = QTEType.Mash;
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private float requiredValue = 10f;

    [Header("Mash")]
    [SerializeField] private float perPressValue = 1f;

    [Header("Hold")]
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
            HoldPerSecond = holdPerSecond
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
