using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnBack;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        btnBack.onClick.AddListener(OnClickBack);
        IsOpen = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // ===== 입력 차단 =====
        EventBus.Publish(new GlobalInputLockRequestedEvent());

        var audio = AudioManager.Instance;
        if (audio == null) return;

        // ===== 슬라이더 초기값 =====
        masterSlider.SetValueWithoutNotify(audio.GetMasterVolume());
        bgmSlider.SetValueWithoutNotify(audio.GetBgmVolume());
        sfxSlider.SetValueWithoutNotify(audio.GetSfxVolume());

        // ===== 슬라이더 바인딩 =====
        masterSlider.onValueChanged.AddListener(audio.SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(audio.SetBgmVolume);
        sfxSlider.onValueChanged.AddListener(audio.SetSfxVolume);
    }

    private void OnDisable()
    {
        // ===== 입력 복구 =====
        EventBus.Publish(new GlobalInputLockReleasedEvent());

        // ===== 리스너 정리 =====
        masterSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }

    public void Show()
    {
        if (IsOpen) return;

        IsOpen = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!IsOpen) return;

        IsOpen = false;
        gameObject.SetActive(false);
    }

    private void OnClickBack()
    {
        EventBus.Publish(new HideSettingsPopupEvent());
    }
}


