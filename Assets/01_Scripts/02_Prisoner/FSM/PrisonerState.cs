public enum PrisonerState
{
    Idle,           // 평상시 (침대에 앉아 있음)
    Inspection,     // 점호 (창살 앞으로 나와서 대기)
    Combat,         // 공격 (반항형 - 플레이어 추적 및 공격)
    Cower,          // 위축 (순응형 - 맞을 때 웅크림)
    Dead            // 제압 (래그돌 상태)
}