using TMPro;
using UnityEngine;

public class MissionTextBinder : MonoBehaviour
{
    [Header("Mission Text Key")]
    [SerializeField] private string textId;

    [Header("Target")]
    [SerializeField] private TMP_Text target;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();

        Refresh();
    }

    public void Refresh()
    {
        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission == null)
            return;

        int missionNo = mission.MissionTextNo;

        target.text = TextManager.Instance.GetMissionText(
            missionNo,
            textId
        );
    }

}
