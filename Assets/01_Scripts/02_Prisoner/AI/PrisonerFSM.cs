using UnityEngine;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Points")]
    public Transform InspectionPoint; // 문 앞 위치

    private IPrisonerState _currentState;

    // 상태 객체들
    public PrisonerIdleState IdleState { get; private set; }
    public PrisonerInspectionState InspectionState { get; private set; }
    public PrisonerCombatState CombatState { get; private set; } // 공격 로직 클래스
    public PrisonerCowerState CowerState { get; private set; }   // 웅크리기 로직 클래스
    public PrisonerDeadState DeadState { get; private set; }     // 사망 로직 클래스
    public PrisonerActor actor;

    public bool IsInvulnerable => _currentState is PrisonerIdleState;

    private void Awake()
    {
        // 상태 초기화
        IdleState = new PrisonerIdleState(this);
        InspectionState = new PrisonerInspectionState(this);
        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update() => _currentState?.Update();

    public void ChangeState(IPrisonerState newState)
    {
        if (_currentState == newState) return;
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void OnDamaged(int dmg, Vector3 hitPoint, Vector3 hitDir)
    {
        _currentState?.OnDamaged(dmg, hitPoint, hitDir);
    }
}