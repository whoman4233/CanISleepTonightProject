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
    [SerializeField] private string crouchDownParameterName = "CrouchDown";
    [SerializeField] private string standUpParameterName = "StandUp";
    [SerializeField] private string isCrouchingParameterName = "IsCrouching";


    public int SpeedParameterHash { get; private set; }
    public int JumpParameterHash { get; private set; }
    public int IsFallingParameterHash { get; private set; }
    public int LandParameterHash { get; private set; }
    public int AttackParameterHash { get; private set; }
    public int DieParameterHash { get; private set; }
    public int MoveXParameterHash { get; private set; }
    public int MoveYParameterHash { get; private set; }
    public int CrouchDownParameterHash { get; private set; }
    public int StandUpParameterHash { get; private set; }
    public int IsCrouchingParameterHash { get; private set; }
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
        CrouchDownParameterHash = Animator.StringToHash(crouchDownParameterName);
        StandUpParameterHash = Animator.StringToHash(standUpParameterName);
        IsCrouchingParameterHash = Animator.StringToHash(isCrouchingParameterName);
    }
}