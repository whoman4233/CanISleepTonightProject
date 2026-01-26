using UnityEngine;
using System;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep { WaitForPlayer, Talking, WaitForSoap, Ambush }

    private BikiniStep _currentStep;
    private GameObject _soapObject;

    private const string SOAP_OBJ_NAME = "SoapTrap";
    private const string DIALOGUE_KEY = "DIAL_BIKINI_TRAP";
    private const float DETECT_RANGE = 4.0f;
    private const int AMBUSH_DAMAGE = 30;

    // ============================================================
    // ★ [핵심] GC 방지용 델리게이트 캐싱 변수
    // ============================================================
    private Action<Mission03DialogueEnded> _onDialogueEndedHandler;

    public PrisonerBikiniState(PrisonerFSM fsm) : base(fsm)
    {
        // ============================================================
        // 생성자에서 핸들러 연결 (메모리 할당 1회만 발생)
        // ============================================================
        _onDialogueEndedHandler = OnDialogueEnded;
    }

    public override void Enter()
    {
        base.Enter();
        _currentStep = BikiniStep.WaitForPlayer;

        // 1. 이동 정지
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // 2. 비누 오브젝트 세팅
        if (_soapObject == null)
        {
            var t = fsm.transform.Find(SOAP_OBJ_NAME);
            if (t != null) _soapObject = t.gameObject;
        }
        if (_soapObject != null) _soapObject.SetActive(false);

        // ============================================================
        // ★ [핵심] 캐싱된 변수로 이벤트 구독 (GC 발생 X)
        // ============================================================
        EventBus.Subscribe(_onDialogueEndedHandler);

        Debug.Log($"[Bikini] {Controller.name}: 유혹 작전 대기 중");
    }

    public override void Update()
    {
        if (player == null) return;

        switch (_currentStep)
        {
            case BikiniStep.WaitForPlayer:
                if (Vector3.Distance(fsm.transform.position, player.position) <= DETECT_RANGE)
                {
                    RequestDialogueStart();
                }
                else
                {
                    LookAtPlayer();
                }
                break;

            case BikiniStep.Talking:
                // 이벤트 수신 대기 (시선 처리만 수행)
                LookAtPlayer();
                break;

            case BikiniStep.WaitForSoap:
                if (_soapObject != null && !_soapObject.activeSelf)
                {
                    ExecuteAmbush();
                }
                break;
        }
    }

    // ============================================================
    // 로직 메서드
    // ============================================================

    private void RequestDialogueStart()
    {
        _currentStep = BikiniStep.Talking;

        if (DialogueManager.Instance != null)
        {
            // DialogueManager.Instance.StartDialogue(DIALOGUE_KEY); 
            Debug.Log($"[Bikini] 대화 시작 요청: {DIALOGUE_KEY}");
        }
    }

    // ============================================================
    // 이벤트 콜백 함수 (Event Callback)
    // ============================================================
    private void OnDialogueEnded(Mission03DialogueEnded eventData)
    {
        // 이미 다른 상태로 넘어갔거나(전투 등), Talking 단계가 아니면 무시
        if (_currentStep != BikiniStep.Talking) return;

        Debug.Log("[Bikini] 대화 종료 이벤트 수신 -> 비누 투척");
        DropSoap();
    }

    private void DropSoap()
    {
        _currentStep = BikiniStep.WaitForSoap;

        if (_soapObject != null)
        {
            _soapObject.SetActive(true);
        }
        else
        {
            fsm.ChangeState(fsm.CombatState);
        }
    }

    private void ExecuteAmbush()
    {
        _currentStep = BikiniStep.Ambush;

        Vector3 backPos = player.position - (player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;

        if (Agent != null) Agent.enabled = false;
        fsm.transform.position = backPos;
        fsm.transform.LookAt(player.position);
        if (Agent != null) Agent.enabled = true;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerHP -= AMBUSH_DAMAGE;

        Anim.SetTrigger("Attack");
        fsm.ChangeState(fsm.CombatState);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    // ============================================================
    // 종료 및 피격 처리
    // ============================================================

    public override void Exit()
    {
        // ============================================================
        // 캐싱된 변수로 구독 해제 (안전함)
        // ============================================================
        EventBus.Unsubscribe(_onDialogueEndedHandler);

        Controller.StopActionBehavior();

        if (_soapObject != null) _soapObject.SetActive(false);
        Anim.SetBool("IsSuspicious", false);

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Controller.StopActionBehavior();

        if (_soapObject != null) _soapObject.SetActive(false);

        // fsm.ChangeState -> Exit() 호출 -> Unsubscribe 자동 수행
        fsm.ChangeState(fsm.CombatState);
    }
}