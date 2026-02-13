using System.Collections.Generic;
using UnityEngine;

public class ImageRegistry : MonoBehaviour
{
    public static ImageRegistry Instance;
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
        // TextManager가 아직 없더라도 등록 예약 (이벤트 기반이므로 안전)
        TextManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        TextManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    public void RegisterImage(ImageInteractable item)
    {
        if (item == null) return;
        _registeredImages.Add(item);
    }

    public void UnregisterImage(ImageInteractable item)
    {
        _registeredImages.Remove(item);
    }

    private void HandleLanguageChanged()
    {
        // Instance 존재 여부 확인
        if (TextManager.Instance == null) return;

        Language newLang = TextManager.Instance.CurrentLanguage;

        foreach (var imageItem in _registeredImages)
        {
            if (imageItem != null)
            {
                imageItem.UpdateImage(newLang);
            }
        }
        Debug.Log($"[ImageRegistry] {newLang}으로 이미지 {_registeredImages.Count}개 일괄 변경.");
    }
}