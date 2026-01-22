using UnityEngine;

public class QTEDistanceTrigger : MonoBehaviour
{
    [SerializeField] private QTEActionSO action;
    [SerializeField] private float triggerDistance = 1.8f;
    [SerializeField] private bool oneShot = true;
    [SerializeField] private Transform player;

    private bool _armed;
    private bool _used;

    private void Update()
    {
        if (!_armed) return;
        if (_used && oneShot) return;
        if (player == null || action == null) return;

        float sqrDist = (player.position - transform.position).sqrMagnitude;
        if (sqrDist > triggerDistance * triggerDistance)
            return;

        TriggerQTE();
    }

    public void Arm()
    {
        _armed = true;
    }

    public void Disarm()
    {
        _armed = false;
    }

    private void TriggerQTE()
    {
        _used = true;
        _armed = false;

        PrisonerQTEContext.SetAttacker(transform);

        EventBus.Publish(new QTEStartedEvent
        {
            Action = action
        });

        if (oneShot)
            enabled = false;
    }
}

