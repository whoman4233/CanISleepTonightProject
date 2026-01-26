using UnityEngine;

public class OutroTimelineReceiver : MonoBehaviour
{
    // Timeline SignalReceiver에서 호출
    public void OnOutroTimelineFinished()
    {
        Debug.Log("[Outro] Timeline Finished");
        EventBus.Publish(new OutroFinishedEvent());
    }
}
