using UnityEngine;
using System;
using System.Collections;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep { WaitForPlayer, Talking, DropSequence, WaitForSoap, AmbushSequence }

    [SerializeField] private BikiniStep _currentStep;

    private GameObject _targetInteractableObject;
    private GameObject _soapRootObject;

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
        _currentStep = BikiniStep.WaitForPlayer;

        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
        Anim.SetBool("Run", false);
        Anim.SetInteger("ActionType", 0);

        // 오브젝트 찾기
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

        if (_soapRootObject != null)
        {
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
        if (player == null) return;

        // ================================================================
        // ★ [핵심 수정] 기습 공격 중이 아니라면 무조건 플레이어를 바라봄
        // ================================================================
        if (_currentStep != BikiniStep.AmbushSequence)
        {
            LookAtPlayer();
        }

        // 상태별 로직 처리
        switch (_currentStep)
        {
            case BikiniStep.WaitForPlayer:
                if (Vector3.Distance(fsm.transform.position, player.position) <= DETECT_RANGE)
                {
                    RequestDialogueStart();
                }
                break;

            case BikiniStep.Talking:
                // 대화 중에는 별도 로직 없음 (시선은 위에서 처리됨)
                break;

            case BikiniStep.WaitForSoap:
                // 비누 감지 로직
                if (_targetInteractableObject != null)
                {
                    // 실제 상호작용 오브젝트가 꺼졌는지 확인
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

        // 비누 활성화 및 Collider 강제 켜기
        if (_soapRootObject != null)
        {
            _soapRootObject.SetActive(true);
            if (_targetInteractableObject != null) _targetInteractableObject.SetActive(true);

            var colliders = _soapRootObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = true;

            Debug.Log($"[Bikini] 비누 활성화 & Collider {colliders.Length}개 켜짐");
        }

        _currentStep = BikiniStep.WaitForSoap;
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator CoExecuteAmbush()
    {
        _currentStep = BikiniStep.AmbushSequence; // 이제부터 회전 로직(LookAtPlayer) 중단

        if (Agent != null) Agent.enabled = false;

        // 플레이어 등 뒤로 이동
        Vector3 backPos = player.position - (player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;
        fsm.transform.position = backPos;

        // 공격 방향으로 즉시 정렬
        fsm.transform.LookAt(player.position);

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
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    public override void Exit()
    {
        EventBus.Unsubscribe(_onDialogueEndedHandler);
        Controller.StopActionBehavior();

        if (_soapRootObject != null) _soapRootObject.SetActive(false);

        Anim.SetBool("IsLuring", false);
        Anim.SetBool("IsTalking", false);
        Anim.SetBool("Run", false);

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        fsm.ChangeState(fsm.CombatState);
    }
}