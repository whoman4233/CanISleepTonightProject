using System;
using TMPro;
using UnityEngine;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI floor1Text;
    [SerializeField] private TextMeshProUGUI floor2Text;

    private PrisonManager _cellManager;

    private Action<ResultUIShowRequestedEvent> _onResultUIShow;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    // 컨텍스트 준비 이벤트 핸들러 캐시
    private Action<GameContextReadyEvent> _onContextReady;

    private void Awake()
    {
        _cellManager = FindObjectOfType<PrisonManager>();
        _onResultUIShow = OnResultUIShow;
        //_onContextReady = OnGameContextReady;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onResultUIShow);

        // 씬 재로딩 이후 새 인스턴스 기준점을 받기 위해 구독
        EventBus.Subscribe(_onContextReady);
        EventBus.Subscribe(_onPhaseChanged);

        // Refresh();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onResultUIShow);
        EventBus.Unsubscribe(_onContextReady);
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    // =========================
    // Context Ready
    // =========================
    //private void OnGameContextReady(GameContextReadyEvent e)
    //{
    //    // 씬 재로딩으로 PrisonCellManager 인스턴스가 바뀌었을 수 있으니 재획득
    //    _cellManager = FindObjectOfType<PrisonCellManager>();
    //    Refresh();
    //}

    private void OnResultUIShow(ResultUIShowRequestedEvent e)
    {
        // 정산 직후 1회 보정(기존 유지)
        Refresh();
    }

    private void Refresh()
    {
        if (_cellManager == null)
        {
            _cellManager = FindObjectOfType<PrisonManager>();
            if (_cellManager == null)
                return;
        }

        // PrisonCellManager가 계산한 값을 그대로 사용
        floor1Text.text = _cellManager.ActiveCell1f.ToString();
        floor2Text.text = _cellManager.ActiveCell2f.ToString();

        if (GameManager.Instance != null)
        {
            
            dayText.text = $"Day : {GameManager.Instance.CurrentDay} / {GameManager.Instance.MaxDay}";
        }
    }

    // =========================
    // Phase 변경
    // =========================
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Standby)
        {
            Refresh(); // Day가 증가한 직후
        }
    }
}





