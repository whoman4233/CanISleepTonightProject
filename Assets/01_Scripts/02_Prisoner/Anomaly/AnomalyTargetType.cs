public enum AnomalyTargetType
{
    Slot,           // 빈 공간에 생성

    // --- 구조물 ---
    CellWall_Left,
    CellWall_Right,
    CellWall_Front,
    CellFloor,
    SteelBarred
}

public enum VisualAnomalyType
{
    None = 0,
    // [3일차]
    BikiniModel,
    GoatHead,
    // [4일차]
    Imposter_Guard,     // 사수 복장
    Imposter_NoBeard,   // 수염 없음
    Imposter_Earring    // 귀걸이 등
}