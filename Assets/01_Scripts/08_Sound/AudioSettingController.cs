using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerSlider : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string exposedParam;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void Start()
    {
        slider.SetValueWithoutNotify(1f);   // 최대 볼륨에서 시작
        mixer.SetFloat(exposedParam, 0f);   // 0 dB
    }

    private void OnValueChanged(float value)
    {
        float db = Mathf.Lerp(-80f, 0f, value);
        mixer.SetFloat(exposedParam, db);
    }
}


