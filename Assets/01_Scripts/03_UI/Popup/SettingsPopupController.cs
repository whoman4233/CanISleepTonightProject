using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    private const string LookSensitivitySliderPrefKey = "Settings.LookSensitivitySlider01";
    private const float DefaultSlider01 = 0.35f;

    // 슬라이더 UI는 0~1로 고정 (감도는 POVInput에서 곡선 변환)
    private const float SliderMin = 0f;
    private const float SliderMax = 1f;

    [Header("Buttons")]
    [SerializeField] private Button btnBack;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("Camera")]
    [SerializeField] private Slider lookSensitivitySlider;
    [SerializeField] private CinemachinePOVInput povInput;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(OnClickBack);

        IsOpen = false;
    }

    private void OnEnable()
    {
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        StartCoroutine(InitSlidersNextFrame());
    }

    private void OnDisable()
    {
        EventBus.Publish(new GlobalInputLockReleasedEvent());

        if (masterSlider != null) masterSlider.onValueChanged.RemoveAllListeners();
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (uiSlider != null) uiSlider.onValueChanged.RemoveAllListeners();

        if (lookSensitivitySlider != null)
            lookSensitivitySlider.onValueChanged.RemoveAllListeners();
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
        if (lookSensitivitySlider == null)
            yield break;

        lookSensitivitySlider.minValue = SliderMin;
        lookSensitivitySlider.maxValue = SliderMax;
        lookSensitivitySlider.wholeNumbers = false;

        if (povInput == null)
            povInput = FindObjectOfType<CinemachinePOVInput>();

        float saved01 = PlayerPrefs.GetFloat(LookSensitivitySliderPrefKey, DefaultSlider01);

        // UI 반영
        lookSensitivitySlider.SetValueWithoutNotify(saved01);

        // 즉시 적용
        if (povInput != null)
            povInput.SetLookSensitivityFromSlider(saved01);

        // 리스너 등록 (중복 등록 방지)
        lookSensitivitySlider.onValueChanged.RemoveAllListeners();
        lookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivitySliderChanged);
    }

    private void OnLookSensitivitySliderChanged(float slider01)
    {
        if (povInput != null)
            povInput.SetLookSensitivityFromSlider(slider01);

        PlayerPrefs.SetFloat(LookSensitivitySliderPrefKey, slider01);
        PlayerPrefs.Save();
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
