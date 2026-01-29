public enum PrisonerType
{
    nervous,
    muscular,
    tattooed,
    Intelligent,
    None
}

public enum PrisonerAIType
{
    Good,
    Bad,

    // 1일차 소음 미션용
    Singing,        // 노래 부르기
    Mumbling,
    Screaming,      // 고함 지르기
    HammeringWall,  // 벽 망치질
    Deadlift,
    Crying,

    // [3일차 추가]
    Attacking,  // 공격하는 죄수 (바로 Combat)
    Graffiti,   // 낙서하는 죄수
    Escaping,   // 탈주하는 죄수 (감방 밖으로 뜀)
    Digging,   // 땅 파는 죄수

    Ambusher,    // 기습하는 놈 (Enemy)

    Suss, 
    QTE_Attacker
}