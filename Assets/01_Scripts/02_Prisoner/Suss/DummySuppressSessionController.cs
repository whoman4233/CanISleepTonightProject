using System.Collections.Generic;
using UnityEngine;

public class DummySuppressSessionController : MonoBehaviour
{
    [Header("Dummy Prisoners")]
    [SerializeField] private int prisonerCount = 1;       // 기획 기본 1명
    [SerializeField] private int hitDamage = 10;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode hitKey = KeyCode.F9;     // 한 명 때리기
    [SerializeField] private KeyCode hitAllKey = KeyCode.F10; // 전원 때리기

    [Header("Data")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;

    [Tooltip("CSV의 PrisonerID (종류 ID)")]
    [SerializeField] private string testPrisonerTemplateId = "P_01";

    private string _cellId;
    private List<DummyPrisonerRuntime> _prisoners;
    private PrisonerGroupTracker _tracker;

    private PrisonerDefinition _cachedTemplate;

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

        if (prisonerDatabase == null)
        {
            Debug.LogError("[DummySuppress] PrisonerDatabaseSO가 연결되지 않았습니다.");
            return;
        }

        if (!prisonerDatabase.TryGet(testPrisonerTemplateId, out _cachedTemplate))
        {
            Debug.LogError($"[DummySuppress] TemplateId를 찾을 수 없습니다: {testPrisonerTemplateId}");
            return;
        }

        _cellId = cellId;

        _prisoners = new List<DummyPrisonerRuntime>(prisonerCount);
        var instanceIds = new List<string>(prisonerCount);

        // 동일 템플릿을 prisonerCount만큼 생성 (확장 대비)
        for (int i = 0; i < prisonerCount; i++)
        {
            // 개체 ID: 감방 + 템플릿 + 순번
            string instanceId = $"{cellId}_{_cachedTemplate.templateId}_{i + 1:00}";
            instanceIds.Add(instanceId);

            // DummyPrisonerRuntime은 (instanceId, PrisonerDefinition) 생성자를 사용
            _prisoners.Add(new DummyPrisonerRuntime(instanceId, _cachedTemplate));
        }

        _tracker = new PrisonerGroupTracker(cellId, instanceIds);

        Debug.Log(
            $"[DummySuppress] START cell={cellId} " +
            $"template={_cachedTemplate.templateId} type={_cachedTemplate.type} " +
            $"count={prisonerCount} hp={_cachedTemplate.hp} atk={_cachedTemplate.atk} spd={_cachedTemplate.spd}"
        );
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
                    Debug.Log($"[DummySuppress] HIT {_prisoners[i].InstanceId} dmg={hitDamage} hp={_prisoners[i].Hp}");
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
                Debug.Log($"[DummySuppress] HIT {p.InstanceId} dmg={hitDamage} hp={p.Hp}");
            }
        }
    }

    private void Cleanup()
    {
        _tracker?.Dispose();
        _tracker = null;
        _prisoners = null;
        _cellId = null;
        _cachedTemplate = null;
    }
}
