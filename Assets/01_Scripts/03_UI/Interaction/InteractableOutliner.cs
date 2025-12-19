using System.Collections.Generic;
using UnityEngine;

public sealed class InteractableOutliner : MonoBehaviour
{
    private const float OutlineOn = 1f;
    private const float OutlineOff = 0f;

    private const string OutlinePropertyName = "_OutlineOn";
    private static readonly int OutlineOnId = Shader.PropertyToID(OutlinePropertyName);

    [Header("Preview / Default")]
    [SerializeField] private bool outlineEnabled = false;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    // 캐시 갱신 조건(계층/렌더러 변화 감지)
    private int _cachedChildCount;
    private int _cachedAllRendererCount;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        CacheRenderersIfNeeded(force: true);
        Apply(outlineEnabled);
    }

    public void SetHighlight(bool isOn)
    {
        outlineEnabled = isOn;
        Apply(isOn);
    }

    private void Apply(bool isOn)
    {
        if (_renderers == null || _renderers.Length == 0) return;
        _mpb ??= new MaterialPropertyBlock();

        float value = isOn ? OutlineOn : OutlineOff;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(OutlineOnId, value);
            r.SetPropertyBlock(_mpb);
        }
    }

    private void CacheRenderersIfNeeded(bool force)
    {
        int currentChildCount = transform.childCount;

        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        int currentAllRendererCount = all.Length;

        if (!force &&
            _renderers != null && _renderers.Length > 0 &&
            _cachedChildCount == currentChildCount &&
            _cachedAllRendererCount == currentAllRendererCount)
        {
            return;
        }

        _cachedChildCount = currentChildCount;
        _cachedAllRendererCount = currentAllRendererCount;

        // 아웃라인 프로퍼티가 있는 머티리얼을 가진 Renderer만 필터링
        List<Renderer> filtered = new List<Renderer>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;

            Material[] mats = r.sharedMaterials; // 인스턴스화 방지
            if (mats == null) continue;

            bool hasOutlineMaterial = false;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null) continue;

                if (mat.HasProperty(OutlineOnId) || mat.HasProperty(OutlinePropertyName))
                {
                    hasOutlineMaterial = true;
                    break;
                }
            }

            if (hasOutlineMaterial)
                filtered.Add(r);
        }

        _renderers = filtered.ToArray();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;

        _mpb ??= new MaterialPropertyBlock();
        CacheRenderersIfNeeded(force: false);
        Apply(outlineEnabled);
    }
#endif
}