using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialNPC;

public class TutorialEventTrigger : MonoBehaviour
{
    public TutorialSubStep stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)

    public void Interact(Player player) // 그냥 오브젝트에 상호작용하면 바로 스텝 넘어가게
    {
        EventBus.Publish(new TutorialStepChangedEvent(stepToPublish));
    }
}
