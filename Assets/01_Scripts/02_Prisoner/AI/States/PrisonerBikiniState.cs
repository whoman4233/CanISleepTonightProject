using UnityEngine;
using System;
using System.Collections;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep { WaitForPlayer, Talking, DropSequence, WaitForSoap, AmbushSequence }

    [SerializeField] private BikiniStep _currentStep;

    private GameObject _targetInteractableObject;
    private GameObject _soapRootObject;

    // ★ [추가] 부모 분리 전, 원래 부모를 기억해두기 위한 변수
    private Transform _soapOriginalParent;

    private const string SOAP_OBJ_NAME = "SoapTrap";
    private const string DIALOGUE_KEY = "DIAL_BIKINI_TRAP";
    private const float DETECT_RANGE = 4.0f;
    private const int AMBUSH_DAMAGE = 30;

    private Action<Mission03DialogueEnded> _onDialogueEndedHandler;

    public PrisonerBikiniState(PrisonerFSM fsm) : base(fsm)
    {
        _onDialogueEndedHandler = OnDialogueEnded;
    }

    public override void Enter()
    {
        base.Enter();
        RefreshPlayerReference();
        _currentStep = BikiniStep.WaitForPlayer;

        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
        Anim.SetBool("Run", false);
        Anim.SetInteger("ActionType", 0);

        // 1. 오브젝트 찾기
        if (_soapRootObject == null)
        {
            var allTransforms = fsm.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == SOAP_OBJ_NAME)
                {
                    _soapRootObject = t.gameObject;
                    break;
                }
            }
        }

        // 2. 초기화 및 원래 부모 기억
        if (_soapRootObject != null)
        {
            // ★ [추가] 나중에 복구하기 위해 원래 부모(아마도 손이나 골반) 캐싱
            _soapOriginalParent = _soapRootObject.transform.parent;

            var interactable = _soapRootObject.GetComponentInChildren<MissionItemInteractable>(true);
            if (interactable != null) _targetInteractableObject = interactable.gameObject;
            else _targetInteractableObject = _soapRootObject;

            _soapRootObject.SetActive(false);
        }

        EventBus.Subscribe(_onDialogueEndedHandler);
        Anim.SetBool("IsLuring", true);
    }

    public override void Update()
    {
        if (Player == null) return;

        // ================================================================
        // ★ [핵심] 기습 공격 전까지는 계속 플레이어를 쳐다봄 (비누는 분리되어서 안 돌아감)
        // ================================================================
        if (_currentStep != BikiniStep.AmbushSequence)
        {
            LookAtPlayer();
        }

        switch (_currentStep)
        {
            case BikiniStep.WaitForPlayer:
                if (Vector3.Distance(fsm.transform.position, Player.position) <= DETECT_RANGE)
                {
                    RequestDialogueStart();
                }
                break;

            case BikiniStep.Talking:
                break;

            case BikiniStep.WaitForSoap:
                if (_targetInteractableObject != null)
                {
                    if (!_targetInteractableObject.activeInHierarchy)
                    {
                        Debug.Log("[Bikini] ★ 비누 사라짐 감지 성공! -> 기습 시작");
                        fsm.StartCoroutine(CoExecuteAmbush());
                    }
                }
                break;
        }
    }

    private void RequestDialogueStart()
    {
        _currentStep = BikiniStep.Talking;
        Anim.SetBool("IsLuring", false);
        Anim.SetBool("IsTalking", true);

        if (DialogueManager.Instance != null)
        {
            // DialogueManager.Instance.StartDialogue(DIALOGUE_KEY); 
            Debug.Log($"[Bikini] 대화 시작 요청");
        }
    }

    private void OnDialogueEnded(Mission03DialogueEnded eventData)
    {
        if (_currentStep != BikiniStep.Talking) return;
        fsm.StartCoroutine(CoDropSoapSequence());
    }

    private IEnumerator CoDropSoapSequence()
    {
        _currentStep = BikiniStep.DropSequence;

        Anim.SetBool("IsTalking", false);
        Anim.SetTrigger("DoDrop");

        yield return new WaitForSeconds(0.5f);

        // 3. 비누 활성화 및 부모 분리 (Detach)
        if (_soapRootObject != null)
        {
            _soapRootObject.SetActive(true);

            // ★ [핵심 수정] 부모를 null로 설정하여 월드 좌표계로 보냄
            // 이제 죄수가 회전해도 비누는 제자리에 가만히 있습니다.
            _soapRootObject.transform.SetParent(null);

            if (_targetInteractableObject != null) _targetInteractableObject.SetActive(true);

            var colliders = _soapRootObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = true;

            Debug.Log($"[Bikini] 비누 활성화 & 부모 분리(Detach) 완료");
        }

        _currentStep = BikiniStep.WaitForSoap;
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator CoExecuteAmbush()
    {
        _currentStep = BikiniStep.AmbushSequence; // 회전 로직 중단

        if (Agent != null) Agent.enabled = false;

        Vector3 backPos = Player.position - (Player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;
        fsm.transform.position = backPos;

        fsm.transform.LookAt(Player.position);

        if (Agent != null) Agent.enabled = true;

        Anim.SetTrigger("DoAttack");

        yield return new WaitForSeconds(0.3f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHP -= AMBUSH_DAMAGE;
        }

        yield return new WaitForSeconds(0.5f);

        fsm.Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (Player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Debug.DrawRay(fsm.transform.position + Vector3.up, dir * 2f, Color.red);
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    public override void Exit()
    {
        EventBus.Unsubscribe(_onDialogueEndedHandler);
        Controller.StopActionBehavior();

        // 4. [안전장치] 상태 종료 시(사망, 피격 등) 비누를 원래 부모에게 복구
        if (_soapRootObject != null)
        {
            _soapRootObject.SetActive(false);

            // 원래 부모가 기억되어 있다면 복구, 없다면 fsm(죄수 본체)으로라도 복구
            if (_soapOriginalParent != null)
            {
                _soapRootObject.transform.SetParent(_soapOriginalParent);
            }
            else
            {
                _soapRootObject.transform.SetParent(fsm.transform);
            }

            // 위치/회전 초기화 (다음 사용을 위해)
            _soapRootObject.transform.localPosition = Vector3.zero;
            _soapRootObject.transform.localRotation = Quaternion.identity;
        }

        Anim.SetBool("IsLuring", false);
        Anim.SetBool("IsTalking", false);
        Anim.SetBool("Run", false);

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 시 Exit()가 호출되면서 비누도 자동으로 회수됨
        fsm.ChangeState(fsm.CombatState);
    }
}