using System.Collections;
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

    private GameManager gameManager;

    private void OnEnable()
    {
        //gameManager = GameContext.Instance.Get<GameManager>();
        GameManager.Instance.OnInGameTimeUpdated += OnTimeUpdated;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            GameManager.Instance.OnInGameTimeUpdated -= OnTimeUpdated;

        StopWarning();
    }

    private void OnTimeUpdated(float seconds)
    {
        timeText.text = FormatTime(seconds);

        if (seconds < 60f)
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

    private string FormatTime(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int millis = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);

        return $"{minutes:00}:{secs:00}.{millis:00}";
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

