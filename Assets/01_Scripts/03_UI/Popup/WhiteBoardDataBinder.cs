using System;
using TMPro;
using UnityEngine;

public class WhiteBoardDataBinder : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI floor1Text;
    [SerializeField] private TextMeshProUGUI floor2Text;

    private PrisonCellManager _cellManager;

    // 정산 완료 / Result UI 표시 시점을 갱신 트리거로만 사용
    private Action<ResultUIShowRequestedEvent> _onResultUIShow;

    private void Awake()
    {
        _cellManager = FindObjectOfType<PrisonCellManager>();
        _onResultUIShow = OnResultUIShow;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onResultUIShow);

        // 이미 활성화된 상태라면 즉시 한 번 반영
        Refresh();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onResultUIShow);
    }

    private void OnResultUIShow(ResultUIShowRequestedEvent e)
    {
        // 정산 직후 한 번 더 보정
        Refresh();
    }

    private void Refresh()
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

        if (GameManager.Instance != null)
        {
            dayText.text = GameManager.Instance.CurrentDay.ToString();
        }
    }
}



