using UnityEngine;
using System.Collections;

public class PrisonerBikiniState : BasePrisonerState
{
    private enum BikiniStep { WaitForPlayer, Talking, WaitForSoap, Ambush }

    private BikiniStep _currentStep;
    private GameObject _soapObject;
    private float _timer;

    // 설정값
    private const string SOAP_OBJ_NAME = "SoapTrap";
    private const string DIALOGUE_KEY = "DIAL_BIKINI_TRAP"; // 나중에 데이터팀이 채워넣을 키
    private const float DETECT_RANGE = 4.0f;
    private const float TALK_DURATION = 3.0f; // 대화 연출 시간 (이 시간 뒤 비누 떨굼)
    private const int AMBUSH_DAMAGE = 30;

    public PrisonerBikiniState(PrisonerFSM fsm) : base(fsm) { }

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

        // 2. 비누 오브젝트 세팅 (자식 오브젝트 찾기)
        if (_soapObject == null)
        {
            var t = fsm.transform.Find(SOAP_OBJ_NAME);
            if (t != null) _soapObject = t.gameObject;
        }
        if (_soapObject != null) _soapObject.SetActive(false);

        // 3. 유혹 애니메이션
        Anim.SetBool("IsSuspicious", true);
        Debug.Log($"[Bikini] {Controller.name}: 유혹 작전 시작");
    }

    public override void Update()
    {
        if (player == null) return;

        switch (_currentStep)
        {
            case BikiniStep.WaitForPlayer:
                if (Vector3.Distance(fsm.transform.position, player.position) <= DETECT_RANGE)
                {
                    FireDialogueEvent(); // 대화 요청만 딱 보내기
                }
                else
                {
                    LookAtPlayer();
                }
                break;

            case BikiniStep.Talking:
                // 대화 시스템 상태를 확인하지 않고, 시간으로 대충 떼움 (로직 분리)
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    DropSoap();
                }
                LookAtPlayer();
                break;

            case BikiniStep.WaitForSoap:
                // 플레이어가 비누를 주워서(SetActive false) 꺼지면 기습
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

    private void FireDialogueEvent()
    {
        _currentStep = BikiniStep.Talking;
        _timer = TALK_DURATION; // 3초 카운트 시작

        // ★ 요청하신 대로 "이벤트 호출"만 해둡니다. (구현은 다른 담당자 몫)
        if (DialogueManager.Instance != null)
        {
            // 오버로딩 함수가 없다면 이 부분만 나중에 수정하시면 됩니다.
            // DialogueManager.Instance.StartDialogue(DIALOGUE_KEY); 
            Debug.Log($"[Bikini] 대화 이벤트 호출함: {DIALOGUE_KEY}");
        }
        else
        {
            Debug.LogWarning("[Bikini] 대화 매니저가 없어서 호출 생략 (테스트 진행)");
        }
    }

    private void DropSoap()
    {
        _currentStep = BikiniStep.WaitForSoap;
        if (_soapObject != null)
        {
            _soapObject.SetActive(true);
            Debug.Log("[Bikini] 비누 투척 완료");
        }
        else
        {
            // 비누 없으면 바로 전투로
            fsm.ChangeState(fsm.CombatState);
        }
    }

    private void ExecuteAmbush()
    {
        _currentStep = BikiniStep.Ambush;

        // 1. 뒤잡기 이동
        Vector3 backPos = player.position - (player.forward * 0.8f);
        backPos.y = fsm.transform.position.y;

        if (Agent != null) Agent.enabled = false;
        fsm.transform.position = backPos;
        fsm.transform.LookAt(player.position);
        if (Agent != null) Agent.enabled = true;

        // 2. 데미지 처리
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerHP -= AMBUSH_DAMAGE;

        Debug.Log("[Bikini] 기습 성공! 데미지 적용됨.");

        // 3. 전투 전환
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

    public override void Exit()
    {
        if (_soapObject != null) _soapObject.SetActive(false);
        Anim.SetBool("IsSuspicious", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (_soapObject != null) _soapObject.SetActive(false);
        fsm.ChangeState(fsm.CombatState);
    }
}