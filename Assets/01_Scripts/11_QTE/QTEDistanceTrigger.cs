using UnityEngine;

public class QTEDistanceTrigger : MonoBehaviour
{
    [Header("QTE Action")]
    [SerializeField] private QTEActionSO action;

    [Header("Distance Settings")]
    [SerializeField] private float triggerDistance = 1.8f;
    [SerializeField] private bool oneShot = true;

    [Header("Refs")]
    [SerializeField] private Transform player;

    private bool _used;

    private void Awake()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (_used && oneShot)
            return;

        if (player == null || action == null)
            return;

        float sqrDist = (player.position - transform.position).sqrMagnitude;
        if (sqrDist > triggerDistance * triggerDistance)
            return;

        TriggerQTE();
    }

    private void TriggerQTE()
    {
        _used = true;

        // 공격자 컨텍스트 세팅 (죄수 기준)
        PrisonerQTEContext.SetAttacker(transform);

        EventBus.Publish(new QTEStartedEvent
        {
            QTEId = action.qteId,
            Config = new QTEConfig
            {
                Type = action.type,
                TimeLimit = action.timeLimit,
                RequiredValue = action.requiredValue,
                PerPressValue = action.perPressValue,
                DecayDelay = action.decayDelay,
                DecayPerSecond = action.decayPerSecond
            }
        });

        if (oneShot)
            enabled = false;
    }
}
