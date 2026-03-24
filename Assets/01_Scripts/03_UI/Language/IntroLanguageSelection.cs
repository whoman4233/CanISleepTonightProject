using UnityEngine;

public class IntroLanguageSelection : MonoBehaviour
{
    private const string HasShownLanguagePopupKey = "HasShownLanguagePopup";
    private const int PopupShownValue = 1;
    private const int PopupNotShownValue = 0;

    [Header("Language Selection UI Root")]
    [SerializeField] private GameObject languageSelectionRoot;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 게임 실행 시 처음 1회만 언어 선택 팝업을 보여준다.
    /// </summary>
    private void Initialize()
    {
        if (languageSelectionRoot == null)
        {
            Debug.LogError("[IntroLanguageSelection] LanguageSelection Root가 할당되지 않았습니다.");
            return;
        }

        bool hasShownPopup = PlayerPrefs.GetInt(HasShownLanguagePopupKey, PopupNotShownValue) == PopupShownValue;
        languageSelectionRoot.SetActive(!hasShownPopup);
    }

    /// <summary>
    /// 한국어 버튼 클릭 시 호출
    /// </summary>
    public void OnClickKorean()
    {
        MarkPopupAsShown();
        HidePopup();

        Debug.Log("[IntroLanguageSelection] Korean 선택");

        // 실제 언어 변경 코드가 있다면 여기에 연결
        // 예:
        // LanguageManager.Instance.SetLanguage(LanguageType.Korean);
    }

    /// <summary>
    /// 영어 버튼 클릭 시 호출
    /// </summary>
    public void OnClickEnglish()
    {
        MarkPopupAsShown();
        HidePopup();

        Debug.Log("[IntroLanguageSelection] English 선택");

        // 실제 언어 변경 코드가 있다면 여기에 연결
        // 예:
        // LanguageManager.Instance.SetLanguage(LanguageType.English);
    }

    /// <summary>
    /// X 버튼 클릭 시 호출
    /// 무엇을 누르든 처음 한 번만 뜨게 할 것이므로 닫기에도 저장 처리
    /// </summary>
    public void OnClickClose()
    {
        MarkPopupAsShown();
        HidePopup();
    }

    /// <summary>
    /// 팝업을 이미 본 것으로 저장
    /// </summary>
    private void MarkPopupAsShown()
    {
        PlayerPrefs.SetInt(HasShownLanguagePopupKey, PopupShownValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 팝업 비활성화
    /// </summary>
    private void HidePopup()
    {
        if (languageSelectionRoot == null)
        {
            Debug.LogError("[IntroLanguageSelection] LanguageSelection Root가 할당되지 않았습니다.");
            return;
        }

        languageSelectionRoot.SetActive(false);
    }
}