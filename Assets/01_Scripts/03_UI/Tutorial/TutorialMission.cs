using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class TutorialMission : MonoBehaviour
{
    [SerializeField] private Image dialogue, boardSee, boxOpened, batonEquip, npcHit, bookClose;
    private Dictionary<DialogueKeys.DialogueType, Image> _missionMap;
    private Image _currentActivePanel;

    private Action<DialogueStepChangedEvent> _stepChanged;

    [Header("Localization Guide Texts")]
    [SerializeField] private TextMeshProUGUI tutorialDialogueText;    // M01
    [SerializeField] private TextMeshProUGUI boardText;    // M02
    [SerializeField] private TextMeshProUGUI boxText;  // M03
    [SerializeField] private TextMeshProUGUI batonText; // M04 
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI bookText;


    private void Awake()
    {
        InitMissionMap();

        UpdateMissionUI(DialogueKeys.DialogueType.Dialogue);

        _stepChanged = e =>
        {
            if (this == null || !gameObject.activeInHierarchy)  // 이 객체가 파괴되었거나 꺼져있다면 이벤트를 무시합니다.(튜토리얼 언로드 후 게임 재시작 시 기존의 튜토리얼 미션을 무력화)
            {
                Debug.LogWarning("튜토리얼 미션 이벤트 핸들러 확인 필요");
                return;
            }
            UpdateMissionUI(e.NewStep);
            StopAllCoroutines();
            StartCoroutine(SafeRefresh(e.NewStep));
            Debug.Log("튜토리얼 미션 UI 교체 완료");
        };
    }
    //private void OnMissionStepChanged(DialogueStepChangedEvent e)
    //{
    //    //StartCoroutine(ForceUpdateAtFrameEnd(e.NewStep));
    //    UpdateMissionUI(e.NewStep);
    //    StopAllCoroutines();
    //    StartCoroutine(SafeRefresh(e.NewStep));
    //    Debug.Log("튜토리얼 미션 UI 교체 완료");
    //}

    private void OnEnable()
    {
        // 구독 전 혹시 남아있을지 모를 중복 구독을 먼저 제거함 (방어 코드)
        EventBus.Unsubscribe<DialogueStepChangedEvent>(_stepChanged);
        EventBus.Subscribe<DialogueStepChangedEvent>(_stepChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DialogueStepChangedEvent>(_stepChanged);
    }

    private void UpdateMissionUI(DialogueKeys.DialogueType step)
    {
        if (_missionMap == null) InitMissionMap();
        if (!_missionMap.TryGetValue(step, out Image target)) return;
        if (_currentActivePanel == target) return;

        // 즉시 교체
        if (_currentActivePanel != null) _currentActivePanel.gameObject.SetActive(false);

        target.gameObject.SetActive(true);
        _currentActivePanel = target;

        // SetActive 직후 Rebuild를 호출해야 즉시 렌더링 큐에 들어갑니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(target.rectTransform);
    }
    

    private void InitMissionMap()
    {
        RefreshLocalizedTexts();

        if (_missionMap != null) return;
        _missionMap = new Dictionary<DialogueKeys.DialogueType, Image>
        {
            { DialogueKeys.DialogueType.Dialogue, dialogue },
            { DialogueKeys.DialogueType.BoardSee, boardSee },
            { DialogueKeys.DialogueType.BoxOpened, boxOpened },
            { DialogueKeys.DialogueType.BatonEquipped, batonEquip },
            { DialogueKeys.DialogueType.NPCHit, npcHit },
            //{ DialogueKeys.DialogueType.BookRead, npcHit },
            { DialogueKeys.DialogueType.BookClose, bookClose }
        };
        foreach (var panel in _missionMap.Values) panel.gameObject.SetActive(false);
    }
    //private IEnumerator ForceUpdateAtFrameEnd(DialogueKeys.DialogueType step)
    //{

    //    yield return new WaitForEndOfFrame();

    //    UpdateMissionUI(step);

    //    // 강제로 UI 캔버스를 다시 그리게 하여 갱신을 확정
    //    Canvas.ForceUpdateCanvases();
    //    Debug.Log($"[Tutorial] 미션 UI 강제 갱신 완료: {step}");
    //}
    private IEnumerator SafeRefresh(DialogueKeys.DialogueType step)
    {
        // 혹시라도 렌더링이 누락되었을 경우를 대비해 마지막 프레임에 한 번 더 체크
        yield return new WaitForEndOfFrame();
        if (_currentActivePanel != null && !_currentActivePanel.gameObject.activeSelf)
        {
            _currentActivePanel.gameObject.SetActive(true);
        }
    }

    private void RefreshLocalizedTexts()
    {
        // 기획자 CSV의 TutorialTextID 열에 적힌 값 그대로 사용
        if (tutorialDialogueText != null) tutorialDialogueText.text = TextManager.Instance.GetTutorialText("Ttxt_M_01");
        if(boardText != null) boardText.text = TextManager.Instance.GetTutorialText("Ttxt_M_02");
        if(boxText != null) boxText.text = TextManager.Instance.GetTutorialText("Ttxt_M_03");
        if(batonText != null) batonText.text = TextManager.Instance.GetTutorialText("Ttxt_M_04");
        if(hitText != null) hitText.text = TextManager.Instance.GetTutorialText("Ttxt_M_05");
        if(bookText != null) bookText.text = TextManager.Instance.GetTutorialText("Ttxt_M_06");

    }
}
