// ================================
// 미션 런타임 상태 컨테이너
// - ScriptableObject에서 분리된 순수 런타임 데이터
// ================================
using System.Collections.Generic;

public class MissionRuntimeState
{
    // Mission 4 전용
    public HashSet<string> usedDialogueTriggerIds = new();
    public bool killedRealFrank;
    public bool immediateFailTriggered;

    public void Reset()
    {
        usedDialogueTriggerIds.Clear();
        killedRealFrank = false;
        immediateFailTriggered = false;
    }
}
