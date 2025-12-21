using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerGroundData
{
    [field: SerializeField]
    [field: Range(0f, 25f)]
    public float BaseSpeed { get; private set; } = 5f;

    [field: SerializeField]
    [field: Range(0f, 25f)]
    public float BaseRotationDamping { get; private set; } = 1f;

    [field: Header("WalkData")]
    [field: SerializeField]
    [field: Range(0f, 2f)]
    public float WalkSpeedModifier { get; private set; } = 0.5f;

    [field: Header("RunData")]
    [field: SerializeField]
    [field: Range(0f, 2f)]
    public float RunSpeedModifier { get; private set; } = 1f;

    [field: Header("CrouchWalkData")]
    [field: SerializeField]
    [field: Range(0f, 2f)]
    public float CrouchWalkSpeedModifier { get; private set; } = 0.3f;

    // CharacterController 캡슐 보정용 데이터
    [field: Header("Controller Capsule (Standing/Crouch)")]
    [field: SerializeField]
    [field: Range(0.5f, 3.0f)]
    public float StandingHeight { get; private set; } = 1.8f;

    [field: SerializeField]
    [field: Range(0.5f, 3.0f)]
    public float CrouchHeight { get; private set; } = 1.2f;

    [field: SerializeField]
    [field: Range(0f, 2.0f)]
    public float StandingCenterY { get; private set; } = 0.9f;

    [field: SerializeField]
    [field: Range(0f, 2.0f)]
    public float CrouchCenterY { get; private set; } = 0.6f;

    [field: SerializeField]
    [field: Range(1f, 30f)]
    public float ColliderLerpSpeed { get; private set; } = 8f;
}

[Serializable]
public class PlayerJumpData
{
    [field: Header("JumpData")]
    [field: SerializeField][field: Range(0f, 25f)] public float JumpForce { get; private set; } = 5f;
}

[Serializable]
public class PlayerAttackData
{
    [field: SerializeField] public List<AttackInfoData> AttackInfoDatas { get; private set; }
    public int GetAttackInfoCount() { return AttackInfoDatas.Count; }
    public AttackInfoData GetAttackInfoData(int index) { return AttackInfoDatas[index]; }
}

[Serializable]
public class AttackInfoData
{
    [field: SerializeField] public string AttackName { get; private set; }
    [field: SerializeField] public int ComboStateIndex { get; private set; }
    [field: SerializeField][field: Range(0f, 1f)] public float ComboTransitionTime { get; private set; }
    [field: SerializeField][field: Range(0f, 3f)] public float ForceTransitionTime { get; private set; }
    [field: SerializeField][field: Range(-10f, 10f)] public float Force { get; private set; }
    [field: SerializeField] public int Damage;
    [field: SerializeField][field: Range(0f, 1f)] public float Dealing_Start_TransitionTime { get; private set; }
    [field: SerializeField][field: Range(0f, 1f)] public float Dealing_End_TransitionTime { get; private set; }
}

[CreateAssetMenu(fileName = "Player", menuName = "Character/Player")]
public class PlayerSO : ScriptableObject
{
    [field: SerializeField] public PlayerGroundData GroundData { get; private set; }
    [field: SerializeField] public PlayerJumpData JumpData { get; private set; }
    [field: SerializeField] public PlayerAttackData AttakData { get; private set; }
}