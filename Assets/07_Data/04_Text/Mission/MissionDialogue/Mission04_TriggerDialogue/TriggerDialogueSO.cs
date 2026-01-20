using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Trigger Dialogue")]
public class TriggerDialogueSO : ScriptableObject
{
    [Header("Dialogue Lines (Trigger Only)")]
    [SerializeField] private DialogueLine[] lines;
    public DialogueLine[] Lines => lines;
}
