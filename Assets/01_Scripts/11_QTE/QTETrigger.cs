using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QTETrigger : MonoBehaviour
{
    [SerializeField] private QTEActionSO _action;
    [SerializeField] private bool oneShot = true; // 콜라이더에 닿았을 때 QTE가 한번만 실행되도록

    private bool _used;
    public void Initialize(QTEActionSO action)
    {
        _action = action;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && _used)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (_action == null)
        {
            return;
        }

        _used = true;

        EventBus.Publish(new QTEStartedEvent
        {
            QTEId = _action.qteId,
            Config = new QTEConfig
            {
                Type = _action.type,
                TimeLimit = _action.timeLimit,
                RequiredValue = _action.requiredValue,
                PerPressValue = _action.perPressValue,
                DecayDelay = _action.decayDelay,
                DecayPerSecond = _action.decayPerSecond
            }
        });

        // 완전 비활성화
        if (oneShot)
            DisableTrigger();
    }
    private void DisableTrigger()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

}
