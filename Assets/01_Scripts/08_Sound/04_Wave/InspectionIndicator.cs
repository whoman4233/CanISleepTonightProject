using System.Collections;
using UnityEngine;

public class InspectionIndicator : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField] private float onDuration = 0.5f;
    [SerializeField] private float offDuration = 0.5f;
    [SerializeField] private GameObject visualRoot; // ±ôºýÀÏ ´ë»ó (ÀÌÆåÆ® ¸ðµ¨/ÆÄÆ¼Å¬)

    private Coroutine _blinkCoroutine;

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void Stop()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            if (visualRoot != null) visualRoot.SetActive(true);
            yield return new WaitForSeconds(onDuration);

            if (visualRoot != null) visualRoot.SetActive(false);
            yield return new WaitForSeconds(offDuration);
        }
    }
}