using System;
using TMPro;
using UnityEngine;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI floor1Text;
    [SerializeField] private TextMeshProUGUI floor2Text;

    private PrisonCellManager _cellManager;

    // ResultUI 이벤트를 "정산 완료 신호"로만 사용
    private Action<ResultUIShowRequestedEvent> _onResultUIShow;

    private void Awake()
    {
        _cellManager = FindObjectOfType<PrisonCellManager>();
        _onResultUIShow = OnResultUIShow;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onResultUIShow);

        // 이미 Standby 이후라면 즉시 반영
        RefreshFromCellManager();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onResultUIShow);
    }

    private void OnResultUIShow(ResultUIShowRequestedEvent e)
    {
        // 정산 시점에 한 번 더 보정 (선택사항)
        RefreshFromCellManager();
    }

    private void RefreshFromCellManager()
    {
        if (_cellManager == null)
        {
            _cellManager = FindObjectOfType<PrisonCellManager>();
            if (_cellManager == null)
                return;
        }

        int floor1 = 0;
        int floor2 = 0;

        foreach (var cell in _cellManager.Cells)
        {
            if (!cell.IsSuspicious)
                continue;

            if (cell.Floor == 1)
                floor1++;
            else if (cell.Floor == 2)
                floor2++;
        }

        floor1Text.text = floor1.ToString();
        floor2Text.text = floor2.ToString();
    }
}

