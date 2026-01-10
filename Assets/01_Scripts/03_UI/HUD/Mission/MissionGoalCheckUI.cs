using UnityEngine;

public class MissionGoalCheckUI : MonoBehaviour
{
    [SerializeField] private GameObject checkOn;
    [SerializeField] private GameObject checkOff;

    public void SetChecked(bool value)
    {
        checkOn.SetActive(value);
        checkOff.SetActive(!value);
    }
}
