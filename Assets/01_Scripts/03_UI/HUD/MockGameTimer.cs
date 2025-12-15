using UnityEngine;

public class MockGameTimer : MonoBehaviour
{
    [Header("Game Time Config")]
    [SerializeField] private float maxGameSeconds = 8 * 60f; // 게임 시간 기준 (8분 = 480초)
    [SerializeField] private float gameMinutesPerRealSecond = 1f; // 1초 = 게임 1분

    private float remainingGameSeconds;
    private float timeScale = 1f;

    private void Start()
    {
        remainingGameSeconds = maxGameSeconds;
    }

    private void Update()
    {
        if (remainingGameSeconds <= 0f)
            return;

        // 게임 시간 감소
        remainingGameSeconds -= Time.deltaTime * 60f * gameMinutesPerRealSecond * timeScale;
        remainingGameSeconds = Mathf.Max(0f, remainingGameSeconds);

        // UI에는 "현실 시간 기준 초"를 전달
        float uiSeconds = remainingGameSeconds / 60f;

        EventBus.Publish(new GameTimeUpdateEvent
        {
            Seconds = uiSeconds
        });
    }

    // UI 기준 "분" → 게임 시간 초로 변환
    private void SetUiMinutes(float uiMinutes)
    {
        remainingGameSeconds = uiMinutes * 60f * 60f;
    }

    // ======================
    // Debug GUILayout
    // ======================
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 400, 400), GUI.skin.window);
        GUILayout.Label("Mock Game Timer Debug");

        GUILayout.Space(10);

        GUILayout.Label($"Game Seconds : {(int)remainingGameSeconds}");
        GUILayout.Label($"UI Seconds    : {(remainingGameSeconds / 60f):F2}");

        GUILayout.Space(10);

        GUILayout.Label("Time Scale");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("x1")) timeScale = 1f;
        if (GUILayout.Button("x10")) timeScale = 10f;
        if (GUILayout.Button("x60")) timeScale = 60f;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label("Jump To Time");

        if (GUILayout.Button("Set 8 Minutes (Start)"))
            SetUiMinutes(8f);

        if (GUILayout.Button("Set 1 Minute (Warning)"))
            SetUiMinutes(1f);

        if (GUILayout.Button("Set 50 Seconds"))
            remainingGameSeconds = 50f * 60f; // UI 50초

        if (GUILayout.Button("Set 10 Seconds (Critical)"))
            remainingGameSeconds = 10f * 60f; // UI 10초

        if (GUILayout.Button("Set 0 (Time Over)"))
            remainingGameSeconds = 0f;


        GUILayout.EndArea();
    }
}




