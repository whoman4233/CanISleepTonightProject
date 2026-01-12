using UnityEngine;
using System.Collections;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep
    {
        WaitForPlayer,  // 1. 플레이어가 방에 들어오길 대기
        Talking,        // 2. 다이얼로그 출력 중 (공격 안 함)
        WaitForSoap,    // 3. 비누를 떨구고 플레이어가 줍기를 대기
        Ambush          // 4. 뒤잡기 및 데미지 처리
    }

    private BikiniStep _currentStep;
    private GameObject _soapObject;
    private float _dialogueTimer = 0f;
    private const float DIALOGUE_DURATION = 3.0f; // 다이얼로그 매니저 없을 시 임시 대기 시간

    // 설정값
    private float _detectRange = 3.0f; // 방 안으로 들어왔다고 판단하는 거리
    private int _ambushDamage = 30;    // 뒤잡기 데미지

    public PrisonerBikiniState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _currentStep = BikiniStep.WaitForPlayer;

        // 1. 초기화: 이동 정지
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // 2. 비누 오브젝트 찾기 (Controller 하위나, 미리 할당된 오브젝트 찾기)
        // (Tip: 죄수 모델 하위에 'SoapTrap'이라는 이름의 오브젝트를 미리 넣어두고 꺼져있게 설정하세요)
        if (_soapObject == null)
        {
            var soapTransform = fsm.transform.Find("SoapTrap");
            if (soapTransform != null) _soapObject = soapTransform.gameObject;
        }

        if (_soapObject != null) _soapObject.SetActive(false);

        // 3. 플레이어 바라보기 애니메이션 (Idle)
        Anim.SetBool("IsSuspicious", true);
    }

    public override void Update()
    {
        if (player == null) return;

        switch (_currentStep)
        {
            // [단계 1] 플레이어 접근 대기
            case BikiniStep.WaitForPlayer:
                float dist = Vector3.Distance(fsm.transform.position, player.position);
                if (dist <= _detectRange)
                {
                    StartDialogue();
                }
                else
                {
                    // 플레이어 쪽을 계속 쳐다봄
                    LookAtPlayer();
                }
                break;

            // [단계 2] 대화 진행 (다이얼로그 출력)
            case BikiniStep.Talking:
                // 실제 DialogueManager가 있다면: if (!DialogueManager.Instance.IsPlaying) NextStep();
                // 지금은 임시 타이머로 처리
                _dialogueTimer -= Time.deltaTime;
                LookAtPlayer(); // 대화 중에도 플레이어 응시

                if (_dialogueTimer <= 0f)
                {
                    DropSoap();
                }
                break;

            // [단계 3] 비누 줍기 대기 (함정 발동 대기)
            case BikiniStep.WaitForSoap:
                // 비누가 비활성화 되었다면? -> 플레이어가 주웠다는 뜻 (Interaction 로직 가정)
                if (_soapObject != null && !_soapObject.activeSelf)
                {
                    ExecuteBackstab();
                }
                break;

            // [단계 4] 기습 후 처리
            case BikiniStep.Ambush:
                // 전투 상태로 전환
                fsm.ChangeState(fsm.CombatState);
                break;
        }
    }

    public override void Exit()
    {
        Anim.SetBool("IsSuspicious", false);
        // 혹시 비누가 켜져 있다면 끄거나 파괴 (선택 사항)
        if (_soapObject != null) _soapObject.SetActive(false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // ★ 핵심: 작업 치는 도중에 맞으면 바로 전투 태세
        Debug.Log($"[Bikini] {Controller.name}: 아! 왜 때려! (작전 실패 -> 전투 전환)");

        // 비누 함정 취소
        if (_soapObject != null) _soapObject.SetActive(false);

        fsm.ChangeState(fsm.CombatState);
    }

    // ========================================================================
    // 내부 로직 메서드
    // ========================================================================

    private void StartDialogue()
    {
        _currentStep = BikiniStep.Talking;
        _dialogueTimer = DIALOGUE_DURATION;

        // 실제 다이얼로그 출력 로직 연결 필요
        Debug.Log($"[Dialog] 미미: 어머, 신참이야? 귀엽게 생겼네.. 나를 위해 비누 좀 주워줄래?");

        // Anim.SetTrigger("Talk"); // 대화 애니메이션이 있다면 재생
    }

    private void DropSoap()
    {
        _currentStep = BikiniStep.WaitForSoap;
        Debug.Log("[Bikini] 비누를 떨어트렸습니다. (상호작용 대기 중)");

        if (_soapObject != null)
        {
            _soapObject.SetActive(true);
            // 비누 위치를 플레이어 앞쪽 바닥 등에 배치하고 싶다면:
            // _soapObject.transform.position = player.position + player.forward * 1.5f + Vector3.up * 0.1f;
        }
        else
        {
            Debug.LogError("[Bikini] 비누 오브젝트(SoapTrap)를 찾을 수 없습니다! Controller 하위를 확인하세요.");
            // 비누가 없으면 바로 전투로 넘어가는 예외 처리
            fsm.ChangeState(fsm.CombatState);
        }
    }

    private void ExecuteBackstab()
    {
        _currentStep = BikiniStep.Ambush;
        Debug.Log("[Bikini] 걸려들었구나! (순간이동 및 기습)");

        // 1. 순간이동 (플레이어 등 뒤)
        // 플레이어 뒤쪽 1m 지점 계산
        Vector3 backPos = player.position - (player.forward * 1.0f);
        backPos.y = fsm.transform.position.y; // 높이는 유지

        // NavMeshAgent가 켜져 있으면 transform 이동이 막힐 수 있으므로 잠시 끔
        if (Agent != null) Agent.enabled = false;
        fsm.transform.position = backPos;
        fsm.transform.LookAt(player.position); // 플레이어 뒷통수 바라보기
        if (Agent != null) Agent.enabled = true;

        // 2. 데미지 입히기
        // 플레이어 스크립트가 있다면: player.GetComponent<PlayerHealth>()?.TakeDamage(_ambushDamage);
        Debug.Log($"[Combat] 플레이어에게 {_ambushDamage} 데미지! (등짝 스매싱)");

        // 3. 즉시 전투 상태로 이행
        fsm.ChangeState(fsm.CombatState);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }
}