
// --------------------
// Global Input Lock (Request)
// --------------------
public struct GlobalInputLockRequestedEvent     //  요청
{
    public GlobalInputLockReason Reason;
    public GlobalInputLockRequestedEvent(GlobalInputLockReason reason) => Reason = reason;
}

public struct GlobalInputLockReleasedEvent      // 
{
    public GlobalInputLockReason Reason;
    public GlobalInputLockReleasedEvent(GlobalInputLockReason reason) => Reason = reason;
}
