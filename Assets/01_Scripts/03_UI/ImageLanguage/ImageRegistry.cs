using System.Collections.Generic;
using UnityEngine;

public class ImageRegistry : MonoBehaviour
{
    public static ImageRegistry Instance;

    // 현재 씬에서 활성화된 이미지들의 리스트
    private HashSet<ImageInteractable> _registeredImages = new HashSet<ImageInteractable>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // TextManager의 언어 변경 이벤트 구독
        TextManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        TextManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    public void RegisterImage(ImageInteractable item)
    {
        _registeredImages.Add(item);
    }

    public void UnregisterImage(ImageInteractable item)
    {
        _registeredImages.Remove(item);
    }

    private void HandleLanguageChanged()
    {
        Language newLang = TextManager.Instance.CurrentLanguage;

        foreach (var imageItem in _registeredImages)
        {
            if (imageItem != null)
            {
                imageItem.UpdateImage(newLang);
            }
        }

        Debug.Log($"[ImageRegistry] {newLang}으로 이미지 {_registeredImages.Count}개 일괄 변경 완료.");
    }
}