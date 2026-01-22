using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QTETrigger : MonoBehaviour
{
    [SerializeField] private QTEActionSO action;
    [SerializeField] private bool oneShot = true;

    private bool _used;

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && _used)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (action == null)
            return;

        _used = true;

        EventBus.Publish(new QTEStartedEvent
        {
            Action = action
        });

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

