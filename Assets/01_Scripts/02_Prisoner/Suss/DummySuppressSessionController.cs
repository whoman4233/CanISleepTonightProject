using System.Collections.Generic;
using UnityEngine;

public class DummySuppressSessionController : MonoBehaviour
{
    [Header("Dummy Prisoners")]
    [SerializeField] private int prisonerCount = 2;
    [SerializeField] private int prisonerHp = 20;
    [SerializeField] private int hitDamage = 10;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode hitKey = KeyCode.F9;     // 한 명 때리기
    [SerializeField] private KeyCode hitAllKey = KeyCode.F10; // 전원 때리기

    private string _cellId;
    private List<DummyPrisonerRuntime> _prisoners;
    private PrisonerGroupTracker _tracker;

    private void OnEnable()
    {
        PrisonerEventBus.OnSuppressSessionStarted += HandleStart;
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnSuppressSessionStarted -= HandleStart;
        Cleanup();
    }

    private void HandleStart(string cellId)
    {
        // 룰상 동시 1개만, 이미 세션 있으면 무시
        if (_tracker != null) return;

        _cellId = cellId;

        _prisoners = new List<DummyPrisonerRuntime>(prisonerCount);
        var ids = new List<string>(prisonerCount);

        for (int i = 0; i < prisonerCount; i++)
        {
            string pid = $"{cellId}_P{i + 1:00}";
            ids.Add(pid);
            _prisoners.Add(new DummyPrisonerRuntime(pid, prisonerHp));
        }

        _tracker = new PrisonerGroupTracker(cellId, ids);

        Debug.Log($"[DummySuppress] START cell={cellId} prisoners={prisonerCount}");
    }

    private void Update()
    {
        if (_tracker == null || _prisoners == null) return;

        if (Input.GetKeyDown(hitKey))
        {
            // 살아있는 첫 번째 한 명 때리기
            for (int i = 0; i < _prisoners.Count; i++)
            {
                if (_prisoners[i].IsAlive)
                {
                    _prisoners[i].TakeDamage(hitDamage);
                    Debug.Log($"[DummySuppress] HIT {_prisoners[i].PrisonerId} dmg={hitDamage} hp={_prisoners[i].Hp}");
                    break;
                }
            }
        }

        if (Input.GetKeyDown(hitAllKey))
        {
            // 전원 때리기(테스트용)
            foreach (var p in _prisoners)
            {
                if (!p.IsAlive) continue;
                p.TakeDamage(hitDamage);
                Debug.Log($"[DummySuppress] HIT {p.PrisonerId} dmg={hitDamage} hp={p.Hp}");
            }
        }
    }

    private void Cleanup()
    {
        _tracker?.Dispose();
        _tracker = null;
        _prisoners = null;
        _cellId = null;
    }
}
