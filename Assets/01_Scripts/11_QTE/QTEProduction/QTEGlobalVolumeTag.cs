using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class QTEGlobalVolumeTag : MonoBehaviour
{
    public Volume Volume { get; private set; }

    private void Awake()
    {
        Volume = GetComponent<Volume>();
        if (Volume == null)
            Debug.LogError("[QTEGlobalVolumeTag] Volume component missing.");
    }
}