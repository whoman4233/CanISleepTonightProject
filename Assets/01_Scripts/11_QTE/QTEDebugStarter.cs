using UnityEngine;

public class QTEDebugStarter : MonoBehaviour
{
    [Header("QTE Debug")]
    [SerializeField] private QTEActionSO testAction;
    [SerializeField] private Transform fakeAttacker;

    [Header("Debug Key")]
    [SerializeField] private KeyCode startKey = KeyCode.Y;
    [SerializeField] private KeyCode successKey = KeyCode.U;
    [SerializeField] private KeyCode failKey = KeyCode.I;

    private void Update()
    {
        if (Input.GetKeyDown(startKey))
        {
            StartQTE();
        }

        if (Input.GetKeyDown(successKey))
        {
            EndSuccess();
        }

        if (Input.GetKeyDown(failKey))
        {
            EndFail();
        }
    }

    private void StartQTE()
    {
        if (testAction == null)
        {
            Debug.LogWarning("[QTEDebug] QTEActionSO 없음");
            return;
        }

        PrisonerQTEContext.SetAttacker(
            fakeAttacker != null ? fakeAttacker : transform
        );

        EventBus.Publish(new QTEStartedEvent
        {
            Action = testAction
        });

        Debug.Log("[QTEDebug] QTE Started (Key)");
    }

    private void EndSuccess()
    {
        EventBus.Publish(new QTEEndedEvent
        {
            Action = testAction,
            Result = QTEResult.Success
        });

        Debug.Log("[QTEDebug] QTE Ended (Success)");
    }

    private void EndFail()
    {
        EventBus.Publish(new QTEEndedEvent
        {
            Action = testAction,
            Result = QTEResult.Fail
        });

        Debug.Log("[QTEDebug] QTE Ended (Fail)");
    }
}
