using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Coroutine 사용을 위해 추가

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("비어있으면 일반 문(단순 개폐)으로 동작합니다.")]
    [SerializeField] private string cellId;

    [SerializeField] private Collider cellInsideTrigger; // ✅ 감방 내부를 덮는 트리거 콜라이더 (옵션)

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry; // ✅ 죄수를 찾기 위해 필요
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [Header("Settings")]
    [SerializeField] private float interactCooldown = 0.8f; // 문 여닫는 쿨타임 (애니메이션 길이와 비슷하게 설정)
    private float _lastInteractTime = -999f; // 마지막 상호작용 시간
    [SerializeField] private bool useRedOutlineOnCloseOnlySlidingDoor = false;
    [SerializeField] private InteractableOutliner outliner;

    [Header("Door SFX")]
    [SerializeField] private AudioClip slidingOpenClip;
    [SerializeField] private AudioClip slidingCloseClip;
    [SerializeField] private AudioClip hingedOpenClip;
    [SerializeField] private AudioClip hingedCloseClip;

    // [상태]
    [SerializeField] private bool _isPlayerInside;
    private bool _isSimpleDoorOpen = false;

    // ★ [추가] 자동 닫힘 코루틴 저장용 변수
    private Coroutine _autoCloseCoroutine;

    private static readonly int OpenHash = Animator.StringToHash("Open");
    private static readonly int CloseHash = Animator.StringToHash("Close");
    private static readonly int LockedHash = Animator.StringToHash("Locked");

    private void Awake()
    {
        if (outliner == null)
            outliner = GetComponentInChildren<InteractableOutliner>(true);

        if (outliner == null)
            outliner = GetComponent<InteractableOutliner>();
    }

    // ★ [추가] 이벤트 구독 (강제 개방 요청 수신)
    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor += HandleForceOpen;
        }
    }

    // ★ [추가] 이벤트 해지
    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor -= HandleForceOpen;
        }
    }

    // ★ [추가] 강제 개방 핸들러
    private void HandleForceOpen(string targetCellId)
    {
        // 내 방 번호가 아니면 무시
        if (this.cellId != targetCellId) return;

        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 요청 수신 (Ambush)");

        // 쿨타임이나 플레이어 상호작용 로직을 무시하고 즉시 문을 엽니다.
        // 기습 상황이므로 InspectionStateMachine은 건드리지 않고 시각적인 개방만 수행합니다.

        // 1. 단순 문 열기 애니메이션 재생
        PlayOpen();

        // 2. 필요하다면 상태 플래그 갱신 (단순 문으로 취급)
        // _isSimpleDoorOpen = true; 
    }

    public void Interact(Player player)
    {
        if (!Validate()) return;

        // 쿨타임 체크 (기존 코드 유지)
        if (Time.time < _lastInteractTime + interactCooldown) return;
        _lastInteractTime = Time.time;

        // 1. 일반 문(계단 문) 로직
        if (string.IsNullOrWhiteSpace(cellId))
        {
            // ★ [추가] 감방 문이 하나라도 열려있는지(점검 중인지) 확인
            if (inspection != null && !string.IsNullOrEmpty(inspection.CurrentInspectingCellId))
            {
                Debug.LogWarning("[Door] 감방 문이 열려있어 계단 문을 열 수 없습니다.");

                // (선택사항) "감방 문을 먼저 닫으세요" 같은 팝업 띄우기
                EventBus.Publish(new ShowTimedTextPopupEvent("감방 문이 열려있습니다! 문을 닫고 이동하세요.", 2.0f));

                // 잠김 애니메이션/소리 재생
                PlayLocked();
                return;
            }

            HandleSimpleDoor();
            return;
        }

        // 2. 감방 전용 로직 (기존 코드 유지)
        HandlePrisonDoor();
    }

    private void HandleSimpleDoor()
    {
        if (!_isSimpleDoorOpen)
        {
            // [열기]
            PlayOpen();
            _isSimpleDoorOpen = true;
            // ★ [추가] 열릴 때 자동 닫힘 코루틴 시작
            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = StartCoroutine(CoAutoCloseSimpleDoor());
        }
        else
        {
            // [닫기]
            PlayClose();
            _isSimpleDoorOpen = false;

            // ★ [추가] 수동으로 닫으면 자동 닫힘 취소
            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        }
    }

    // ★ [추가] 5초 후 자동으로 닫는 코루틴
    private IEnumerator CoAutoCloseSimpleDoor()
    {
        yield return new WaitForSeconds(5.0f);

        // 5초 뒤에도 여전히 열려있다면 닫는다
        if (_isSimpleDoorOpen)
        {
            if (verboseLog) Debug.Log($"[Door] 일반 문 5초 경과하여 자동 닫힘");
            HandleSimpleDoor(); // 닫기 로직 재호출
        }
    }

    private void HandlePrisonDoor()
    {
        // inspection이 null인지 안전장치
        if (inspection == null)
        {
            Debug.LogError($"[Door] {name}: InspectionStateMachine이 연결되지 않았습니다!");
            return;
        }

        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            // [문 열기] 점검 시작
            // [수정 2] TryEnter 실패 원인 파악을 위한 로그 추가
            if (TryEnter())
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
                //CellID 전달 이벤트 발행 -> 문열림 경고 HUD ON
                EventBus.Publish(new CellInspectionInProgressEvent
                {
                    CellId = cellId
                });
                PlayOpen();
                TriggerPrisonerInspection();
            }
            else
            {
                Debug.LogWarning($"[Door] {cellId}: 진입 불가 (TryEnter 실패). 잠김 애니메이션 재생.");
                PlayLocked();
            }
        }
        else
        {
            // ... (기존 닫기 로직 유지)
            if (cellInsideTrigger != null && _isPlayerInside)
            {
                Debug.LogWarning($"[Door] {cellId} 내부에 플레이어가 있어 문을 닫을 수 없습니다.");
                return;
            }

            if (TryExit())
            {
                PlayClose();
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
                //CellID 전달 이벤트 발행 -> 문열림 경고 HUD OFF
                EventBus.Publish(new CellInspectionCompletedEvent
                {
                    CellId = cellId
                });
            }
            else PlayLocked();
        }
    }

    // ✅ 죄수를 찾아 상태를 변경하는 함수
    private void TriggerPrisonerInspection()
    {
        if (contentRegistry == null) return;

        if (contentRegistry.TryGet(cellId, out var content))
        {
            if (content.prisoner != null)
            {
                var fsm = content.prisoner.GetComponent<PrisonerFSM>();
                if (fsm != null)
                {
                    // [수정 전] 강제로 상태를 꽂아버림 (문제 원인)
                    // fsm.ChangeState(fsm.InspectionState);

                    // [수정 후] FSM에게 "점호 시작됐다"고 정중하게 알림
                    // -> FSM 내부의 OnStartInspection()이 실행되면서
                    // -> "나 노래하는 중인데?" 하고 무시(return)하거나, "네 나갑니다" 하고 상태 변경
                    fsm.OnStartInspection();

                    if (verboseLog) Debug.Log($"[Door] {cellId} 죄수에게 점검 신호(OnStartInspection) 전달.");
                }
            }
        }
    }

    private bool TryEnter()
    {
        if (cellManager == null) return false;

        var cell = cellManager.GetCell(cellId);
        if (cell == null)
        {
            Debug.LogError($"[Door] CellManager에서 ID '{cellId}'를 찾을 수 없습니다. 오타 확인 필요.");
            return false;
        }

        // [수정] 재진입을 허용하기 위해 잠금 체크 주석 처리
        /*
        if (cell.IsLockedForDay)
        {
            Debug.Log($"[Door] {cellId}는 금일 폐쇄(IsLockedForDay) 상태입니다.");
            return false;
        }
        */

        // InspectionStateMachine에서 거부하는 경우 (이미 다른 방 점검 중 등)
        bool canEnter = inspection.TryEnterCell(cellId);
        if (!canEnter) Debug.Log($"[Door] InspectionStateMachine.TryEnterCell('{cellId}')가 false를 반환했습니다.");

        return canEnter;
    }

    private bool TryExit() => inspection.RequestExitCell(cellId);

    private void PlayOpen()
    {
        Debug.Log($"[Door] PlayOpen called : {name}");
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(CloseHash);
        doorAnimator.SetTrigger(OpenHash);
        PlayOpenSound();
    }

    private void PlayClose()
    {
        Debug.Log($"[Door] PlayClose called : {name}");
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(OpenHash);
        doorAnimator.SetTrigger(CloseHash);

        if (useRedOutlineOnCloseOnlySlidingDoor && outliner != null)
            outliner.SetHighlight(true, Color.red);
        PlayCloseSound();
    }

    private void PlayLocked()
    {
        if (doorAnimator != null)
            doorAnimator.SetTrigger(LockedHash);

        if (outliner != null) outliner.SetHighlight(true, Color.red);
    }

    private bool Validate()
    {
        if (doorAnimator == null) return false;
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
            if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
            if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
            if (inspection == null || cellManager == null || contentRegistry == null) return false;
        }
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
        }
    }
    /// <summary>
    /// 프롬프트 시스템을 위한 문 상태 요약
    /// - 내부 bool / 외부 상태들을 하나의 enum으로 변환
    /// </summary>
    public OpenClosePromptState GetPromptStateEnum()
    {
        // =========================
        // 1. 일반 문 (cellId 없음)
        // =========================
        if (string.IsNullOrWhiteSpace(cellId))
        {
            return _isSimpleDoorOpen
                ? OpenClosePromptState.Open
                : OpenClosePromptState.Close;
        }

        // =========================
        // 2. 감방 문
        // =========================
        if (inspection == null)
        {
            // 안전장치: inspection이 없으면 닫힌 상태로 취급
            return OpenClosePromptState.Close;
        }

        bool isInspectingThisCell =
            inspection.CurrentInspectingCellId == cellId;

        // 점검 시작 전 → 닫혀 있음 (열기 프롬프트)
        if (!isInspectingThisCell)
        {
            return OpenClosePromptState.Close;
        }

        // 점검 중인데 플레이어가 안에 있으면 닫기 불가
        if (_isPlayerInside)
        {
            return OpenClosePromptState.CannotClose;
        }

        // 그 외는 열려 있음 (닫기 가능)
        return OpenClosePromptState.Open;
    }
    //==========================================
    //문열기 사운드 재생용 함수
    //==========================================
    private void PlayOpenSound()
    {
        AudioClip clip = null;

        if (string.IsNullOrWhiteSpace(cellId))
        {
            // 일반 문 → 여닫이
            clip = hingedOpenClip;
        }
        else
        {
            // 감방 문 → 슬라이딩
            clip = slidingOpenClip;
        }

        if (clip == null)
            return;

        AudioManager.Instance.PlaySFX(clip);
    }
    private void PlayCloseSound()
    {
        AudioClip clip = null;

        if (string.IsNullOrWhiteSpace(cellId))
        {
            clip = hingedCloseClip;
        }
        else
        {
            clip = slidingCloseClip;
        }

        if (clip == null)
            return;

        AudioManager.Instance.PlaySFX(clip);
    }

}