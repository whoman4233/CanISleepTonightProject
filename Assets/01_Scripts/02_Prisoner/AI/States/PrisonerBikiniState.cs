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
    private const string PLEASURE_SFX_KEY = "Bikini_Pleasure";
    private const float DETECT_RANGE = 2.0f;
    private const int AMBUSH_DAMAGE = 30;

    [Header("Throw Settings")]
    [SerializeField] private float _throwForce = 2.0f;
    [SerializeField] private float _upwardModifier = 2.0f;
    [SerializeField] private float _throwHeightOffset = 1.0f;

    private Action<Mission03DialogueEnded> _onDialogueEndedHandler;
    private Vector3 _preCalculatedThrowDir; // ★ 던질 방향 미리 저장용

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
            Agent.updateRotation = false;
        }

        if (Player != null)
        {
            Vector3 initialDir = (Player.position - fsm.transform.position).normalized;
            initialDir.y = 0;
            if (initialDir != Vector3.zero)
                fsm.transform.rotation = Quaternion.LookRotation(initialDir);
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
                fsm.Controller.Data.CurrentHealth = fsm.Controller.Data.MaxHealth;

            LookAtPlayer();
        }

        switch (_currentStep)
        {
            case BikiniStep.WaitForPlayer:
                if (Vector3.Distance(fsm.transform.position, Player.position) <= DETECT_RANGE)
                    RequestDialogueStart();
                break;
            case BikiniStep.WaitForSoap:
                if (_targetInteractableObject != null && !_targetInteractableObject.activeInHierarchy)
                {
                    // ★ [수정] 비누를 주운 직후에 바로 만족스러운 소리 재생
                    if (Controller != null)
                        Controller.PlaySpecialSfx(PLEASURE_SFX_KEY);

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
    }

    private void OnDialogueEnded(Mission03DialogueEnded eventData)
    {
        // 대화가 먼저 끝나버린 경우(WaitForPlayer)도 허용하도록 변경
        if (_currentStep != BikiniStep.Talking && _currentStep != BikiniStep.WaitForPlayer)
            return;

        // 대기 상태에서 대화가 끝났다면 유혹 애니메이션 강제 종료
        Anim.SetBool(IsLuringHash, false);

        // 애니메이션으로 인한 회전 간섭을 피하기 위해, 대화 종료 시점의 방향을 고정
        _preCalculatedThrowDir = (Player.position - fsm.transform.position).normalized;
        _preCalculatedThrowDir.y = 0;

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
            // 1. 부모 해제 및 활성화
            _soapRootObject.transform.SetParent(null);
            _soapRootObject.SetActive(true);

            // 2. [핵심] 발사 위치를 캐릭터 정면 약간 앞 + 위쪽으로 강제 설정
            // 본체 콜라이더와 겹치지 않게 정면 방향으로 0.6m 정도 오프셋을 줍니다.
            Vector3 spawnPos = fsm.transform.position + (_preCalculatedThrowDir * 0.6f);
            spawnPos.y += _throwHeightOffset;
            _soapRootObject.transform.position = spawnPos;

            if (_targetInteractableObject != null)
                _targetInteractableObject.SetActive(true);

            // 콜라이더 활성화
            var colliders = _soapRootObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = true;

            if (_soapRb != null)
            {
                // 3. 물리 상태 초기화 (이전 관성 제거)
                _soapRb.isKinematic = false;
                _soapRb.velocity = Vector3.zero;
                _soapRb.angularVelocity = Vector3.zero;

                // 4. 힘 계산 및 적용
                // 수평으로 날아가는 힘(throwForce)과 위로 띄우는 힘(upwardModifier)을 조합
                Vector3 horizontalForce = _preCalculatedThrowDir * _throwForce;
                Vector3 verticalForce = Vector3.up * _upwardModifier;

                _soapRb.AddForce(horizontalForce + verticalForce, ForceMode.Impulse);

                // 약간의 회전을 주어 자연스럽게 던져지는 연출
                _soapRb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }

            Debug.Log($"[Bikini] 비누 투척 완료: 방향 {_preCalculatedThrowDir}");
        }
    }

    private IEnumerator CoExecuteAmbush()
    {
        _currentStep = BikiniStep.AmbushSequence;
        if (Agent != null) Agent.enabled = false;

        Vector3 backPos = Player.position - (Player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;
        fsm.transform.position = backPos;

        Vector3 lookDir = (Player.position - fsm.transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            fsm.transform.rotation = Quaternion.LookRotation(lookDir);

        if (Agent != null)
        {
            Agent.enabled = true;
            Agent.updateRotation = false;
        }

        // (이곳의 사운드 코드는 Update의 주웠을 때 시점으로 이동했으므로 삭제해도 무방하나 중복 재생 방지를 위해 Update에서만 호출 권장)

        Anim.SetTrigger(DoAttackHash);
        yield return new WaitForSeconds(0.3f);

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerHP -= AMBUSH_DAMAGE;

        if (Player != null)
        {
            // 직접 HP를 수정하지 않고 이벤트를 던집니다.
            // 플레이어의 신음 소리 및 대미지 수치 적용은 이 이벤트를 받는 곳에서 처리됩니다.
            EventBus.Publish(new PlayerDamagedEvent { });

            Debug.Log($"[Bikini] 기습 성공: PlayerDamagedEvent 발행 (Damage: {AMBUSH_DAMAGE})");
        }

        yield return new WaitForSeconds(0.5f);

        if (Agent != null) Agent.updateRotation = true;
        fsm.Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }

    private void LookAtPlayer()
    {
        Vector3 dir = (Player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
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

        if (Agent != null) Agent.updateRotation = true;
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