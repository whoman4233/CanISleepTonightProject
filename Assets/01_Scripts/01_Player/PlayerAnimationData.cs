using UnityEngine;

[System.Serializable]
public class PlayerAnimationData
{
    [Header("Parameters")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string jumpParameterName = "Jump";
    [SerializeField] private string isFallingParameterName = "IsFalling";
    [SerializeField] private string landParameterName = "Land";
    [SerializeField] private string attackParameterName = "Attack";
    [SerializeField] private string dieParameterName = "Die";
    [SerializeField] private string moveXParameterName = "MoveX";
    [SerializeField] private string moveYParameterName = "MoveY";


    public int SpeedParameterHash { get; private set; }
    public int JumpParameterHash { get; private set; }
    public int IsFallingParameterHash { get; private set; }
    public int LandParameterHash { get; private set; }
    public int AttackParameterHash { get; private set; }
    public int DieParameterHash { get; private set; }
    public int MoveXParameterHash { get; private set; }
    public int MoveYParameterHash { get; private set; }
    public void Initialize()
    {
        SpeedParameterHash = Animator.StringToHash(speedParameterName);
        JumpParameterHash = Animator.StringToHash(jumpParameterName);
        IsFallingParameterHash = Animator.StringToHash(isFallingParameterName);
        LandParameterHash = Animator.StringToHash(landParameterName);
        AttackParameterHash = Animator.StringToHash(attackParameterName);
        DieParameterHash = Animator.StringToHash(dieParameterName);
        MoveXParameterHash = Animator.StringToHash(moveXParameterName);
        MoveYParameterHash = Animator.StringToHash(moveYParameterName);
    }
}