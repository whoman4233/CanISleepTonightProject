using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable, IInspectable
{
    [Header("Inspection")]
    [SerializeField] private GameObject inspectPrefab;
    [SerializeField] private GameObject visualRoot;

    [Header("Hidden Items")]
    [SerializeField] private GameObject knifeWorldVisual;
    [SerializeField] private HiddenItemHolder itemHolder;

    private KnifeStateSO knifeState;

    private void Start()
    {
        knifeState = itemHolder.GetItem<KnifeStateSO>();

        if (knifeState == null)
        {
            Debug.LogError("[World] KnifeStateSO null");
            return;
        }

        Debug.Log($"[World] KnifeState ID={knifeState.GetInstanceID()}");

        knifeState.OnFoundStateChanged += OnKnifeFoundChanged;

        // 초기 상태 반영
        OnKnifeFoundChanged(knifeState.IsFound);
    }

    private void OnDestroy()
    {
        if (knifeState != null)
            knifeState.OnFoundStateChanged -= OnKnifeFoundChanged;
    }

    private void OnKnifeFoundChanged(bool isFound)
    {
        Debug.Log($"[World] OnKnifeFoundChanged | isFound={isFound}");

        if (knifeWorldVisual != null)
            knifeWorldVisual.SetActive(!isFound);
    }

    public Transform GetInspectPivot() => transform;
    public GameObject GetInspectPrefab() => inspectPrefab;

    public void OnInspectionEnter()
    {
        visualRoot.SetActive(false);
    }

    public void OnInspectionExit()
    {
        visualRoot.SetActive(true);
    }

    public void Interact(Player player)
    {
        player.TryEnterInspection(this);
    }
}




