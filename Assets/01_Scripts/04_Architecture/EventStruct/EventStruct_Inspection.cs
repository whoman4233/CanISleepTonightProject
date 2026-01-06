
//==========================================
//Inspection(상세보기) 이벤트 목록
//==========================================

public struct InspectionRequestedEvent //상세보기 요청
{
    public IInspectable Target;
}

public struct InspectionStartedEvent // 상세보기 시작
{
    public IInspectable Target;
}

public struct InspectionEndedEvent // 상세보기 종료
{

}


public struct InspectionViewReadyEvent // 상세보기 상호작용을 위한 View 이벤트
{

}

public struct InspectionViewRequestedEvent
{

}

public struct InspectionViewReleasedEvent
{

}