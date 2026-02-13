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

    private void OnEnable()
    {
        // ImageRegistry에 자기 자신을 등록
        if (ImageRegistry.Instance != null)
        {
            ImageRegistry.Instance.RegisterImage(this);
        }

        // 활성화될 때 현재 언어에 맞춰 이미지 초기화
        UpdateImage(TextManager.Instance.CurrentLanguage);
    }

    private void OnDisable()
    {
        // 비활성화될 때 등록 해제
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