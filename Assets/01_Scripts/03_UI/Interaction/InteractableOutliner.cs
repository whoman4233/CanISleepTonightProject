using System.Collections.Generic;
using UnityEngine;

public sealed class InteractableOutliner : MonoBehaviour
{
    private const float OutlineOn = 1f;
    private const float OutlineOff = 0f;

    private const string OutlinePropertyName = "_OutlineOn";
    private const string OutlineColorPropertyName = "_Color";

    private static readonly int OutlineOnId = Shader.PropertyToID(OutlinePropertyName);
    private static readonly int OutlineColorId = Shader.PropertyToID(OutlineColorPropertyName);

    // ✅ Ray에 걸리기 전(기본 숨김) : 검정 + 알파 0
    private static readonly Color HiddenOutlineColor = new Color(0f, 0f, 0f, 0f);

    [Header("Preview / Default")]
    [SerializeField] private bool outlineEnabled = false;

    // 기본색 : 흰색 (On 상태일 때만 사용됨)
    [SerializeField] private Color defaultOutlineColor = new Color(255f, 255f, 255f, 255f);

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private Color _currentOutlineColor;

    private int _cachedChildCount;
    private int _cachedAllRendererCount;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        CacheRenderersIfNeeded(force: true);

        _currentOutlineColor = defaultOutlineColor;

        // ✅ 시작 시 무조건 숨김 상태로 적용(얇은 메쉬 잔상 방지)
        outlineEnabled = false;
        Apply(isOn: false);
    }

    /// <summary>
    /// 기존 호출부(PlayerInteractor 등) 호환용.
    /// -> 색을 "현재 색"으로 유지한 채 on/off만 바꿈
    /// </summary>
    public void SetHighlight(bool isOn)
    {
        outlineEnabled = isOn;
        Apply(isOn);
    }

    /// <summary>
    /// 색까지 바꾸고 싶을 때 사용 (Locked=red 등)
    /// </summary>
    public void SetHighlight(bool isOn, Color color)
    {
        _currentOutlineColor = color;
        outlineEnabled = isOn;
        Apply(isOn);
    }

    public void ResetColorToDefault()
    {
        _currentOutlineColor = defaultOutlineColor;
        Apply(outlineEnabled);
    }

    private void Apply(bool isOn)
    {
        if (_renderers == null || _renderers.Length == 0) return;
        _mpb ??= new MaterialPropertyBlock();

        float outlineValue = isOn ? OutlineOn : OutlineOff;
        Color appliedColor = isOn ? _currentOutlineColor : HiddenOutlineColor;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(OutlineOnId, outlineValue);

            // ✅ OFF일 때도 항상 색을 덮어써서(알파0) 잔상/누수 방지
            _mpb.SetColor(OutlineColorId, appliedColor);

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

        List<Renderer> filtered = new List<Renderer>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;

            Material[] mats = r.sharedMaterials;
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

        _currentOutlineColor = defaultOutlineColor;

        // ✅ 에디터에서도 기본은 숨김(필요하면 outlineEnabled 토글해서 프리뷰)
        Apply(outlineEnabled);
    }
#endif
}