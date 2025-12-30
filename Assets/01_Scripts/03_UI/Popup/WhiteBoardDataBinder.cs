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

    // 정산 완료 / Result UI 표시 시점 트리거
    private Action<ResultUIShowRequestedEvent> _onResultUIShow;

    private void Awake()
    {
        _cellManager = FindObjectOfType<PrisonManager>();
        _onResultUIShow = OnResultUIShow;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onResultUIShow);

        // 이미 활성화된 경우 즉시 반영
        Refresh();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onResultUIShow);
    }

    private void OnResultUIShow(ResultUIShowRequestedEvent e)
    {
        // 정산 직후 1회 보정
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
            dayText.text = GameManager.Instance.CurrentDay.ToString();
        }
    }
}




