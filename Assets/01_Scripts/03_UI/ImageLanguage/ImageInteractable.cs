using UnityEngine;
using UnityEngine.UI;

public class ImageInteractable : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Sprite koreanSprite;
    [SerializeField] private Sprite englishSprite;

    private Image _targetImage;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
    }

    private void Start() // OnEnable 대신 Start 사용 권장 (안전한 참조를 위해)
    {
        // Registry에 등록
        if (ImageRegistry.Instance != null)
        {
            ImageRegistry.Instance.RegisterImage(this);
        }

        // 초기 이미지 설정 (TextManager가 준비되었을 때만)
        if (TextManager.Instance != null)
        {
            UpdateImage(TextManager.Instance.CurrentLanguage);
        }
    }

    private void OnEnable()
    {
        // 만약 오브젝트가 껐다 켜질 때를 대비 (Start는 최초 1회만 실행됨)
        if (ImageRegistry.Instance != null)
        {
            ImageRegistry.Instance.RegisterImage(this);
        }

        // 활성화될 때마다 현재 언어 기준으로 즉시 갱신
        if (TextManager.Instance != null)
        {
            UpdateImage(TextManager.Instance.CurrentLanguage);
        }
    }

    private void OnDisable()
    {
        if (ImageRegistry.Instance != null)
        {
            ImageRegistry.Instance.UnregisterImage(this);
        }
    }

    public void UpdateImage(Language lang)
    {
        if (_targetImage == null) return;
        _targetImage.sprite = (lang == Language.Korean) ? koreanSprite : englishSprite;
    }
}