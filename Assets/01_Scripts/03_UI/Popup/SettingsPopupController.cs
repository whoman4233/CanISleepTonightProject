using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    private enum PanelType
    {
        Sound,
        Mouse,
        Language
    }

    // ===== PlayerPrefs Keys =====
    private const string LookHorizontalPrefKey = "Settings.LookSensitivity.Horizontal01";
    private const string LookVerticalPrefKey = "Settings.LookSensitivity.Vertical01";

    // ===== Defaults (UI Slider 0~1) =====
    private const float DefaultHorizontal01 = 0.35f;
    private const float DefaultVertical01 = 0.35f;

    // ===== Slider Range (UI 0~1 fixed) =====
    private const float SliderMin = 0f;
    private const float SliderMax = 1f;

    [Header("Buttons")]
    [SerializeField] private Button btnBack;

    [Header("Category Panels")]
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject mousePanel;
    [SerializeField] private GameObject languagePanel;

    [Header("Category Buttons")]
    [SerializeField] private Button btnSound;
    [SerializeField] private Button btnMouse;
    [SerializeField] private Button btnLanguage;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("Camera - Look Sensitivity (0~1)")]
    [SerializeField] private Slider lookHorizontalSlider;
    [SerializeField] private Slider lookVerticalSlider;
    [SerializeField] private CinemachinePOVInput povInput;

    public bool IsOpen { get; private set; }
    public bool IsInCategoryRoot { get; private set; }

    private void Awake()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(OnClickBack);

        if (btnSound != null)
            btnSound.onClick.AddListener(() => OpenPanel(PanelType.Sound));

        if (btnMouse != null)
            btnMouse.onClick.AddListener(() => OpenPanel(PanelType.Mouse));

        if (btnLanguage != null)
            btnLanguage.onClick.AddListener(() => OpenPanel(PanelType.Language));

        IsOpen = false;
    }

    private void OnEnable()
    {
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        CloseAllPanels();
        StartCoroutine(InitSlidersNextFrame());
    }

    private void OnDisable()
    {
        EventBus.Publish(new GlobalInputLockReleasedEvent());

        // 오디오 리스너 제거
        if (masterSlider != null) masterSlider.onValueChanged.RemoveAllListeners();
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (uiSlider != null) uiSlider.onValueChanged.RemoveAllListeners();

        // 마우스 감도 리스너 제거
        if (lookHorizontalSlider != null) lookHorizontalSlider.onValueChanged.RemoveAllListeners();
        if (lookVerticalSlider != null) lookVerticalSlider.onValueChanged.RemoveAllListeners();

        // 변경사항 저장 (한 번만)
        PlayerPrefs.Save();
    }

    private void CloseAllPanels()
    {
        if (soundPanel != null) soundPanel.SetActive(false);
        if (mousePanel != null) mousePanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false);
    }

    private void OpenPanel(PanelType type)
    {
        CloseAllPanels();

        switch (type)
        {
            case PanelType.Sound:
                soundPanel?.SetActive(true);
                break;

            case PanelType.Mouse:
                mousePanel?.SetActive(true);
                break;

            case PanelType.Language:
                languagePanel?.SetActive(true);
                break;
        }
    }

    private IEnumerator InitSlidersNextFrame()
    {
        yield return null;

        // ===== 오디오 슬라이더 =====
        var audio = AudioManager.Instance;
        if (audio != null)
        {
            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(audio.GetMasterVolume());
                masterSlider.onValueChanged.AddListener(audio.SetMasterVolume);
            }

            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(audio.GetBgmVolume());
                bgmSlider.onValueChanged.AddListener(audio.SetBgmVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(audio.GetSfxVolume());
                sfxSlider.onValueChanged.AddListener(audio.SetSfxVolume);
            }

            if (uiSlider != null)
            {
                uiSlider.SetValueWithoutNotify(audio.GetUiVolume());
                uiSlider.onValueChanged.AddListener(audio.SetUiVolume);
            }
        }

        // ===== 카메라 감도 슬라이더 =====
        if (povInput == null)
            povInput = FindObjectOfType<CinemachinePOVInput>();

        InitLookSlider(lookHorizontalSlider);
        InitLookSlider(lookVerticalSlider);

        float savedHorizontal01 = PlayerPrefs.GetFloat(LookHorizontalPrefKey, DefaultHorizontal01);
        float savedVertical01 = PlayerPrefs.GetFloat(LookVerticalPrefKey, DefaultVertical01);

        if (lookHorizontalSlider != null)
            lookHorizontalSlider.SetValueWithoutNotify(savedHorizontal01);

        if (lookVerticalSlider != null)
            lookVerticalSlider.SetValueWithoutNotify(savedVertical01);

        // 즉시 적용
        if (povInput != null)
        {
            povInput.SetHorizontalSensitivityFromSlider(savedHorizontal01);
            povInput.SetVerticalSensitivityFromSlider(savedVertical01);
        }

        // 리스너 등록 (중복 방지)
        if (lookHorizontalSlider != null)
        {
            lookHorizontalSlider.onValueChanged.RemoveAllListeners();
            lookHorizontalSlider.onValueChanged.AddListener(OnLookHorizontalChanged);
        }

        if (lookVerticalSlider != null)
        {
            lookVerticalSlider.onValueChanged.RemoveAllListeners();
            lookVerticalSlider.onValueChanged.AddListener(OnLookVerticalChanged);
        }
    }

    private static void InitLookSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = SliderMin;
        slider.maxValue = SliderMax;
        slider.wholeNumbers = false;
    }

    private void OnLookHorizontalChanged(float slider01)
    {
        float t = Mathf.Clamp01(slider01);

        if (povInput != null)
            povInput.SetHorizontalSensitivityFromSlider(t);

        PlayerPrefs.SetFloat(LookHorizontalPrefKey, t);
    }

    private void OnLookVerticalChanged(float slider01)
    {
        float t = Mathf.Clamp01(slider01);

        if (povInput != null)
            povInput.SetVerticalSensitivityFromSlider(t);

        PlayerPrefs.SetFloat(LookVerticalPrefKey, t);
    }

    public void Show()
    {
        IsOpen = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }

    private void OnClickBack()
    {
        EventBus.Publish(new HideSettingsPopupEvent());
    }
}