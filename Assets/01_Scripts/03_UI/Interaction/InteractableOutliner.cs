using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class InteractableOutliner : MonoBehaviour
{
    private static readonly int OutlineOnId = Shader.PropertyToID("_OutlineOn");

    [Header("아웃라인 프로퍼티 세팅")]
    [SerializeField] private string outlinePropertyName = "_OutlineOn";

    [SerializeField]
    [Range(0f, 1f)]
    private float outlineOnValue = 1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float outlineOffValue = 0f;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private bool _isHighlighted;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        if (string.IsNullOrEmpty(outlinePropertyName))
        {
            outlinePropertyName = "_OutlineOn";
        }

        SetHighlight(false);
    }

    public void SetHighlight(bool isOn)
    {
        if (_renderer == null) return;
        if (_isHighlighted == isOn) return;

        _isHighlighted = isOn;

        // 현재 머티리얼의 PropertyBlock 읽어오기
        _renderer.GetPropertyBlock(_propertyBlock);

        float value = isOn ? outlineOnValue : outlineOffValue;

        // 프로퍼티 이름을 직접 쓰고 싶다면 아래처럼 사용 가능:
        // int propId = Shader.PropertyToID(outlinePropertyName);
        // _propertyBlock.SetFloat(propId, value);

        _propertyBlock.SetFloat(OutlineOnId, value);

        _renderer.SetPropertyBlock(_propertyBlock);
    }
}