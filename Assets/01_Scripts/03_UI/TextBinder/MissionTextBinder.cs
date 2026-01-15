using TMPro;
using UnityEngine;

public class MissionTextBinder : MonoBehaviour
{
    [Header("Mission Text Key")]
    [SerializeField] private MissionTextRole role;

    [Header("Target")]
    [SerializeField] private TMP_Text target;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();

        Refresh();
    }
    private void Refresh()
    {
        var mission = DailyMissionManager.Instance?.CurrentMission;
        if (mission == null)
            return;

        target.text = TextManager.Instance.GetMissionText(
            mission.MissionTextNo,
            role
        );
    }
}
