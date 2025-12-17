using UnityEngine;

[System.Serializable]
public class PlayerAnimationData
{
    [Header("Parameters")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string jumpParameterName = "Jump";
    [SerializeField] private string isFallingParameterName = "IsFalling";
    [SerializeField] private string attackParameterName = "Attack";
    [SerializeField] private string dieParameterName = "Die";

    public int SpeedParameterHash { get; private set; }
    public int JumpParameterHash { get; private set; }
    public int IsFallingParameterHash { get; private set; }
    public int AttackParameterHash { get; private set; }
    public int DieParameterHash { get; private set; }

    public void Initialize()
    {
        SpeedParameterHash = Animator.StringToHash(speedParameterName);
        JumpParameterHash = Animator.StringToHash(jumpParameterName);
        IsFallingParameterHash = Animator.StringToHash(isFallingParameterName);
        AttackParameterHash = Animator.StringToHash(attackParameterName);
        DieParameterHash = Animator.StringToHash(dieParameterName);
    }
}