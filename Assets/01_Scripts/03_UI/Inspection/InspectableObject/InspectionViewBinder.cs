using UnityEngine;

public class InspectionViewBinder : MonoBehaviour, IInspectionView
{
    private HiddenItemInspectVisual[] _visuals;

    private void Awake()
    {
        _visuals = GetComponentsInChildren<HiddenItemInspectVisual>(true);
        Debug.Log($"[InspectionViewBinder] Awake visuals={_visuals.Length} on {name}");
    }

    public void Bind(IInspectable inspectable)
    {
        Debug.Log($"[InspectionViewBinder] Bind called. owner={inspectable} on {name}");

        if (inspectable is not InspectableObject worldInspectable)
        {
            Debug.LogError("[InspectionViewBinder] owner is not InspectableObject", this);
            return;
        }

        foreach (var v in _visuals)
            v.BindFromWorld(worldInspectable);
    }
}