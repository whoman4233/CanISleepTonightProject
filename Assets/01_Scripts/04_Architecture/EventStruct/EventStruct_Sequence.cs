using UnityEngine;
// ==========================================
// 연출용 이벤트
// ==========================================
public struct SequencePlayRequestedEvent
{
    public SequenceOptionSO Sequence;
    public Transform TargetPoint;
}
// ==========================================
// 연출용 이벤트(플레이어 위치 이동용)
// ==========================================
public struct PlayerCinematicLockRequestedEvent
{

}

public struct PlayerCinematicLockReleasedEvent
{

}