using UnityEngine;
using System;
using System.Collections;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep { WaitForPlayer, Talking, DropSequence, WaitForSoap, AmbushSequence }

    [SerializeField] private BikiniStep _currentStep;

    private GameObject _targetInteractableObject;
    private GameObject _soapRootObject;
    private Rigidbody _soapRb;
    private Transform _soapOriginalParent;

    private const string SOAP_OBJ_NAME = "PSNW_Soap01";
    private const string DIALOGUE_KEY = "DIAL_BIKINI_TRAP";
    private const string PLEASURE_SFX_KEY = "Bikini_Pleasure"; // ★ 효과음 키값 정의
    private const float DETECT_RANGE = 4.0f;
    private const int AMBUSH_DAMAGE = 30;

    [Header("Throw Settings")]
    [SerializeField] private float _throwForce = 5.0f;
    [SerializeField] private float _upwardModifier = 2.0f;
    [SerializeField] private float _throwHeightOffset = 1.0f;

    private Action<Mission03DialogueEnded> _onDialogueEndedHandler;

    // Animator Hashes
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ActionTypeHash = Animator.StringToHash("ActionType");
    private static readonly int IsLuringHash = Animator.StringToHash("IsLuring");
    private static readonly int IsTalkingHash = Animator.StringToHash("IsTalking");
    private static readonly int DoDropHash = Animator.StringToHash("DoDrop");
    private static readonly int DoAttackHash = Animator.StringToHash("DoAttack");

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
        Anim.SetBool(RunHash, false);
        Anim.SetInteger(ActionTypeHash, 0);

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
            _soapOriginalParent = _soapRootObject.transform.parent;
            _soapRb = _soapRootObject.GetComponent<Rigidbody>();

            var interactable = _soapRootObject.GetComponentInChildren<MissionItemInteractable>(true);
            if (interactable != null) _targetInteractableObject = interactable.gameObject;
            else _targetInteractableObject = _soapRootObject;

            if (_soapRb != null) _soapRb.isKinematic = true;
            _soapRootObject.SetActive(false);
        }

        EventBus.Subscribe(_onDialogueEndedHandler);
        Anim.SetBool(IsLuringHash, true);
    }

    public override void Update()
    {
        if (Player == null) return;

        if (_currentStep != BikiniStep.AmbushSequence)
        {
            if (fsm.Controller != null && fsm.Controller.Data != null)
            {
                fsm.Controller.Data.CurrentHealth = fsm.Controller.Data.MaxHealth;
            }
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
            case BikiniStep.WaitForSoap:
                if (_targetInteractableObject != null && !_targetInteractableObject.activeInHierarchy)
                {
                    Debug.Log("[Bikini] 비누 사라짐 감지 성공! -> 기습 시작");
                    fsm.StartCoroutine(CoExecuteAmbush());
                }
                break;
        }
    }

    private void RequestDialogueStart()
    {
        _currentStep = BikiniStep.Talking;
        Anim.SetBool(IsLuringHash, false);
        Anim.SetBool(IsTalkingHash, true);
        Debug.Log($"[Bikini] 대화 시작 요청");
    }

    private void OnDialogueEnded(Mission03DialogueEnded eventData)
    {
        if (_currentStep != BikiniStep.Talking) return;
        fsm.StartCoroutine(CoDropSoapSequence());
    }

    private IEnumerator CoDropSoapSequence()
    {
        _currentStep = BikiniStep.DropSequence;
        Anim.SetBool(IsTalkingHash, false);
        Anim.SetTrigger(DoDropHash);

        yield return new WaitForSeconds(0.5f);
        ThrowSoap();

        _currentStep = BikiniStep.WaitForSoap;
        yield return new WaitForSeconds(0.5f);
    }

    private void ThrowSoap()
    {
        if (_soapRootObject != null)
        {
            _soapRootObject.SetActive(true);
            _soapRootObject.transform.SetParent(null);

            // Y축 1.0f 높이 보정 (손 높이)
            _soapRootObject.transform.position += Vector3.up * _throwHeightOffset;

            if (_targetInteractableObject != null) _targetInteractableObject.SetActive(true);

            var colliders = _soapRootObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = true;

            if (_soapRb != null)
            {
                _soapRb.isKinematic = false;
                Vector3 throwDir = (fsm.transform.forward + (Vector3.up * 0.5f)).normalized;

                // 물리 힘 가하기 (이후 관여 안함)
                _soapRb.AddForce(throwDir * _throwForce + (Vector3.up * _upwardModifier), ForceMode.Impulse);
                _soapRb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            Debug.Log($"[Bikini] 비누 던지기 완료 (Y offset: {_throwHeightOffset})");
        }
    }

    private IEnumerator CoExecuteAmbush()
    {
        _currentStep = BikiniStep.AmbushSequence;

        if (Agent != null) Agent.enabled = false;

        // 플레이어 뒤쪽으로 순간이동
        Vector3 backPos = Player.position - (Player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;
        fsm.transform.position = backPos;
        fsm.transform.LookAt(Player.position);

        if (Agent != null) Agent.enabled = true;

        // ★ [추가] 뒤를 잡았을 때 효과음 재생
        if (Controller != null)
        {
            Controller.PlaySpecialSfx(PLEASURE_SFX_KEY);
        }

        Anim.SetTrigger(DoAttackHash);

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
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    public override void Exit()
    {
        EventBus.Unsubscribe(_onDialogueEndedHandler);
        Controller.StopActionBehavior();

        if (_soapRootObject != null)
        {
            if (_soapRb != null)
            {
                _soapRb.velocity = Vector3.zero;
                _soapRb.angularVelocity = Vector3.zero;
                _soapRb.isKinematic = true;
            }
            _soapRootObject.SetActive(false);
            _soapRootObject.transform.SetParent(_soapOriginalParent != null ? _soapOriginalParent : fsm.transform);
            _soapRootObject.transform.localPosition = Vector3.zero;
            _soapRootObject.transform.localRotation = Quaternion.identity;
        }

        Anim.SetBool(IsLuringHash, false);
        Anim.SetBool(IsTalkingHash, false);
        Anim.SetBool(RunHash, false);

        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (_currentStep != BikiniStep.AmbushSequence) return;
        fsm.ChangeState(fsm.CombatState);
    }
}