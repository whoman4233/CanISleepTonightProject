using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMission : MonoBehaviour
{
    [SerializeField] private Image dialogue, boardSee, boxOpened, batonEquip, npcHit, bookClose;
    private Dictionary<DialogueKeys.DialogueType, Image> _missionMap;
    private Image _currentActivePanel;



    private void Awake()
    {
        InitMissionMap();
        UpdateMissionUI(DialogueKeys.DialogueType.Dialogue);
    }
    private void OnMissionStepChanged(DialogueStepChangedEvent e)
    {
        StartCoroutine(ForceUpdateAtFrameEnd(e.NewStep));
        Debug.Log("튜토리얼 미션 UI 교체 완료");
    }

    private void OnEnable()
    {
        // 구독 전 혹시 남아있을지 모를 중복 구독을 먼저 제거함 (방어 코드)
        EventBus.Unsubscribe<DialogueStepChangedEvent>(OnMissionStepChanged);
        EventBus.Subscribe<DialogueStepChangedEvent>(OnMissionStepChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DialogueStepChangedEvent>(OnMissionStepChanged);
    }

    private void UpdateMissionUI(DialogueKeys.DialogueType step)
    {
        // 현재 활성 패널이 바꾸려는 패널과 같다면 무시
        if (_missionMap.TryGetValue(step, out Image target))
        {
            if (_currentActivePanel == target) return;

            // 이전 패널 끄기
            if (_currentActivePanel != null) _currentActivePanel.gameObject.SetActive(false);

            // 새 패널 켜기
            target.gameObject.SetActive(true);
            _currentActivePanel = target;
            Canvas.ForceUpdateCanvases();
        }
    }

    private void InitMissionMap()
    {
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
    private IEnumerator ForceUpdateAtFrameEnd(DialogueKeys.DialogueType step)
    {

        yield return new WaitForEndOfFrame();

        UpdateMissionUI(step);

        // 강제로 UI 캔버스를 다시 그리게 하여 갱신을 확정
        Canvas.ForceUpdateCanvases();
        Debug.Log($"[Tutorial] 미션 UI 강제 갱신 완료: {step}");
    }
}
