using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDTimer : MonoBehaviour
{
    [Header("타이머")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColorA = Color.red;
    [SerializeField] private Color warningColorB = Color.yellow;
    [SerializeField] private float warningBlinkSpeed = 0.5f;

    private Coroutine warningCoroutine;
    private bool isWarning;

    private System.Action<GameTimeUpdateEvent> _onTimeUpdated;

    private void Awake()
    {
        _onTimeUpdated = OnTimeUpdated;
        Debug.Log("[HUDTimer] Awake");
    }
    private void OnEnable()
    {
        Debug.Log("[HUDTimer] OnEnable");
        EventBus.Subscribe<GameTimeUpdateEvent>(_onTimeUpdated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameTimeUpdateEvent>(_onTimeUpdated);
        StopWarning();
    }

    //시간 표시 
    private string FormatTime(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int millis = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);

        return $"{minutes:00}:{secs:00}.{millis:00}";
    }


    //60초 미만 시 경고용 
    private void OnTimeUpdated(GameTimeUpdateEvent e)
    {
        Debug.Log($"[HUDTimer] Receive: {e.Seconds}");

        timeText.text = FormatTime(e.Seconds);

        if (e.Seconds < 60f)
        {
            if (!isWarning)
                StartWarning();
        }
        else
        {
            if (isWarning)
                StopWarning();
        }
    }

    private void StartWarning()
    {
        isWarning = true;
        warningCoroutine = StartCoroutine(WarningBlink());
    }

    private void StopWarning()
    {
        isWarning = false;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        timeText.color = normalColor;
    }

    private IEnumerator WarningBlink()
    {
        while (true)
        {
            timeText.color = warningColorA;
            yield return new WaitForSeconds(warningBlinkSpeed);

            timeText.color = warningColorB;
            yield return new WaitForSeconds(warningBlinkSpeed);
        }
    }

}
