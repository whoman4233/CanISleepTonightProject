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
    PSN_FrankeA,
    PSN_FrankeB,
    PSN_FrankeR,
    // 6일차
    Suspect1,
    Suspect2,
    Suspect3
}