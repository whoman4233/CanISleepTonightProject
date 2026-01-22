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
        // 이벤트가 오면 즉시 UI만 바꿈
        EventBus.Subscribe<DialogueStepChangedEvent>(e => UpdateMissionUI(e.NewStep));
        UpdateMissionUI(DialogueKeys.DialogueType.Dialogue);
    }

    private void UpdateMissionUI(DialogueKeys.DialogueType step)
    {
        if (_currentActivePanel != null) _currentActivePanel.gameObject.SetActive(false);
        if (_missionMap.TryGetValue(step, out Image target))
        {
            target.gameObject.SetActive(true);
            _currentActivePanel = target;
        }
    }

    private void InitMissionMap()
    {
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
}
