using UnityEngine;

public interface IQTELookTargetProvider // QTE 바라보는 대상의 Transform 받아오기용 인터페이스
{
    Transform GetQTELookTarget();
}
public class PrisonerQTEAnchor : MonoBehaviour, IQTELookTargetProvider
{
    [Header("QTE Look Target")]
    [SerializeField] private Transform headLookTarget;

    public Transform GetQTELookTarget()
    {
        return headLookTarget != null ? headLookTarget : transform;
    }
}
