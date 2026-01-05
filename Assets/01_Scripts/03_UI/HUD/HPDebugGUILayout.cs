using UnityEngine;

public class HPDebugGUILayout : MonoBehaviour
{
    [Header("Debug Only")]
    [SerializeField] private bool showGui = true;

    private const int HpStep10 = 10;
    private const int HpStep20 = 20;

    private void OnGUI()
    {
        if (!showGui)
            return;

        if (GameManager.Instance == null)
            return;

        GUILayout.BeginArea(new Rect(20, 20, 220, 200), GUI.skin.box);

        GUILayout.Label($"Player HP : {GameManager.Instance.PlayerHP}");

        GUILayout.Space(10);

        if (GUILayout.Button("-10 HP"))
        {
            ChangeHp(-HpStep10);
        }

        if (GUILayout.Button("-20 HP"))
        {
            ChangeHp(-HpStep20);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("+10 HP"))
        {
            ChangeHp(HpStep10);
        }

        if (GUILayout.Button("Reset HP (100)"))
        {
            SetHp(100);
        }

        GUILayout.EndArea();
    }

    private void ChangeHp(int delta)
    {
        GameManager.Instance.PlayerHP += delta;
    }

    private void SetHp(int value)
    {
        GameManager.Instance.PlayerHP = value;
    }
}
