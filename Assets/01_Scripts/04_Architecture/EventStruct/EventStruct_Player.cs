
// ==========================================
// Player 유무 파악
// ==========================================
public struct PlayerPresenceChangedEvent         
{
    public bool IsPresent;
    public PlayerPresenceChangedEvent(bool isPresent) => IsPresent = isPresent;
}

// ==========================================
// Player 생성 이벤트
// ==========================================
public struct PlayerSpawnedEvent 
{
    public Player Player;
}

//==========================================
// 플레이어 HP 변경 이벤트 (HUD 수신)
//==========================================
public struct PlayerHpChangedEvent
{
    public int CurrentHp;

    public PlayerHpChangedEvent(int currentHp)
    {
        CurrentHp = currentHp;
    }
}
//==========================================
// 게임 오버 이벤트
//==========================================
public struct GameOverEvent
{

}