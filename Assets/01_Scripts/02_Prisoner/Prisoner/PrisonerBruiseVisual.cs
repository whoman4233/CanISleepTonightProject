using UnityEngine;

public sealed class PrisonerBruiseVisual : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PrisonerController controller; // 비우면 자동으로 GetComponent 시도

    [Header("Bruise Objects (Drag & Drop)")]
    [SerializeField] private GameObject bruiseStage1; // PSN_Bruise01
    [SerializeField] private GameObject bruiseStage2; // PSN_Bruise02

    [Header("Force Off (Safety)")]
    [SerializeField] private bool forceOffOnAwake = true;
    [SerializeField] private bool forceOffOnEnable = true;
    [SerializeField] private bool forceOffOnStart = true; // 다른 스크립트가 Start에서 켜는 경우 대비

    [Header("Thresholds (Remaining HP Ratio)")]
    private const float OneThird = 1f / 3f;
    private const float TwoThird = 2f / 3f;

    [SerializeField, Range(0f, 1f)] private float stage1Threshold = TwoThird; // 남은 HP <= 2/3 (피 1/3 감소)
    [SerializeField, Range(0f, 1f)] private float stage2Threshold = OneThird; // 남은 HP <= 1/3 (피 2/3 감소)

    [Header("Update")]
    [SerializeField] private float refreshIntervalSeconds = 0.1f;

    [Header("Option")]
    [Tooltip("Stage2가 켜질 때 Stage1도 같이 켭니다(누적 멍 연출).")]
    [SerializeField] private bool stage2AlsoEnablesStage1 = false;

    private float _timer;
    private int _lastStage = -1;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<PrisonerController>();
        }

        if (forceOffOnAwake)
        {
            ForceOffBruises();
        }
    }

    private void OnEnable()
    {
        _timer = 0f;
        _lastStage = -1;

        if (forceOffOnEnable)
        {
            ForceOffBruises();
        }
    }

    private void Start()
    {
        if (forceOffOnStart)
        {
            ForceOffBruises();
        }

        // 시작 시 현재 체력 상태 반영(초기부터 멍이 있어야 하는 특수 케이스 대비)
        UpdateBruiseByHealth();
    }

    private void Update()
    {
        if (controller == null) return;

        _timer += Time.deltaTime;
        if (_timer < refreshIntervalSeconds) return;
        _timer = 0f;

        UpdateBruiseByHealth();
    }

    private void UpdateBruiseByHealth()
    {
        // ====== 여기(데이터 접근)만 네 프로젝트 구조에 맞게 조정하면 됨 ======
        // controller.Data.CurrentHealth / controller.Data.MaxHealth 구조라고 가정
        var data = controller.Data;
        if (data == null || data.MaxHealth <= 0f)
        {
            ApplyStage(0);
            return;
        }

        float ratio = data.CurrentHealth / data.MaxHealth; // 남은 체력 비율
        // ===================================================================

        int stage = CalculateStage(ratio);
        if (stage == _lastStage) return;

        ApplyStage(stage);
        _lastStage = stage;
    }

    private int CalculateStage(float remainingHpRatio)
    {
        // 0: none, 1: bruise1, 2: bruise2
        if (remainingHpRatio <= stage2Threshold) return 2;
        if (remainingHpRatio <= stage1Threshold) return 1;
        return 0;
    }

    private void ApplyStage(int stage)
    {
        // 누적 방식:
        // stage >= 1 이면 bruiseStage1은 계속 켜짐
        // stage >= 2 이면 bruiseStage2도 켜짐
        bool stage1 = stage >= 1;
        bool stage2 = stage >= 2;

        if (bruiseStage1 != null) bruiseStage1.SetActive(stage1);
        if (bruiseStage2 != null) bruiseStage2.SetActive(stage2);
    }

    private void ForceOffBruises()
    {
        if (bruiseStage1 != null) bruiseStage1.SetActive(false);
        if (bruiseStage2 != null) bruiseStage2.SetActive(false);
    }

    [ContextMenu("Force Off Bruises")]
    private void ForceOffBruisesContextMenu()
    {
        ForceOffBruises();
    }
}