using UnityEngine;
using System;

public class QTEDistanceTrigger : MonoBehaviour
{
    [Header("QTE")]
    [SerializeField] private QTEActionSO action;
    [SerializeField] private bool oneShot = true;

    private Transform player;
    private bool _armed;
    private bool _used;

    public bool IsArmed => _armed;
    public bool HasQTE => action != null;

    private Action<PlayerSpawnedEvent> _onPlayerSpawned;
    private Action<PlayerPresenceChangedEvent> _onPlayerPresenceChanged;
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;

    private void Awake()
    {
        _onPlayerSpawned = OnPlayerSpawned;
        _onPlayerPresenceChanged = OnPlayerPresenceChanged;
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPlayerSpawned);
        EventBus.Subscribe(_onPlayerPresenceChanged);
        EventBus.Subscribe(_onInspectionStarted);
        EventBus.Subscribe(_onInspectionEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPlayerSpawned);
        EventBus.Unsubscribe(_onPlayerPresenceChanged);
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);
    }

    // =========================
    // Inspection Events
    // =========================

    /// <summary>
    /// 상세보기 진입 시:
    /// QTE를 가진 죄수라면 무조건 Armed
    /// </summary>
    private void OnInspectionStarted(InspectionStartedEvent e)
    {
        if (_used && oneShot)
            return;

        Arm();
    }

    private void OnInspectionEnded(InspectionEndedEvent e)
    {
        Disarm();
    }

    // =========================
    // Player Events
    // =========================

    private void OnPlayerSpawned(PlayerSpawnedEvent e)
    {
        player = e.Player.transform;
    }

    private void OnPlayerPresenceChanged(PlayerPresenceChangedEvent e)
    {
        if (!e.IsPresent)
        {
            player = null;
            Disarm();
        }
    }

    // =========================
    // External Control
    // =========================

    /// <summary>
    /// QTEApproachController에서 "도착"을 알릴 때 호출됨
    /// </summary>
    public void NotifyArrived()
    {
        if (!_armed) return;
        if (_used && oneShot) return;
        if (action == null) return;

        TriggerQTE();
    }

    // =========================
    // QTE Control
    // =========================

    private void Arm()
    {
        var fsm = GetComponentInParent<PrisonerFSM>();
        if (fsm == null)
            return;

        // Combat / Dead / QTE 접근 중에는 재무장 금지
        if (fsm.CurrentState == fsm.CombatState ||
            fsm.CurrentState == fsm.DeadState ||
            fsm.CurrentState == fsm.QTEApproachState)
            return;

        _armed = true;
        Debug.Log($"[QTE] Armed : {name}", this);
    }


    private void Disarm()
    {
        _armed = false;
    }

    private void TriggerQTE()
    {
        var fsm = GetComponentInParent<PrisonerFSM>();
        if (fsm != null)
        {
            if (fsm.CurrentState != fsm.QTEApproachState)
                return;
        }

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

