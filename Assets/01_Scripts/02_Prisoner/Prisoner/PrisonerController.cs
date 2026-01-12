using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    private const float RagdollImpactForce = 10f;

    // ================================================================
    // [1] 데이터 정의
    // ================================================================

    // ★ VisualSkinData, specialSkins, _skinMap 삭제됨!
    // 프리팹 자체가 외형이므로 내부 교체 로직 불필요.

    [System.Serializable]
    public struct ActionPropData
    {
        public PrisonerAIType type;    // 예: HammeringWall
        public GameObject propObject;  // 예: 손에 쥐어준 망치 (이 프리팹의 자식)
    }

    // ================================================================
    // [2] 컴포넌트 및 변수
    // ================================================================

    public PrisonerData Data { get; private set; }
    public CellAnchor AssignedCell { get; private set; }

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollSetting ragdoll;
    [SerializeField] private PrisonerSfxController sfx;
    private PrisonerFSM fsm;
    private NavMeshAgent agent;

    [Header("Action Props (Tools)")]
    // 이 프리팹이 사용할 도구들 (각 프리팹마다 손 위치에 맞게 세팅 필요)
    [SerializeField] private List<ActionPropData> actionProps;

    private Dictionary<PrisonerAIType, GameObject> _propMap;

    public bool IsSuspicious { get; private set; }
    public PrisonerAIType AIType => Data != null ? Data.RuntimeAIType : PrisonerAIType.Good;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        // ★ 스킨 맵핑 로직 삭제됨

        // 도구 맵핑 (이건 여전히 필요. 비키니 입은 죄수도 망치는 들어야 하니까)
        _propMap = new Dictionary<PrisonerAIType, GameObject>();
        if (actionProps != null)
        {
            foreach (var data in actionProps)
            {
                if (data.propObject != null && !_propMap.ContainsKey(data.type))
                    _propMap.Add(data.type, data.propObject);
            }
        }
    }

    // 스포너에서 호출하는 초기화 함수
    public void Initialize(PrisonerData data, CellAnchor cell, bool isSuspicious)
    {
        this.Data = data;
        this.AssignedCell = cell;
        this.IsSuspicious = isSuspicious;

        // ★ ApplyVisualAnomaly 호출 삭제됨 (이미 맞는 프리팹으로 소환되었음)

        // FSM 초기화
        if (fsm != null)
        {
            fsm.Setup(this, agent, animator);
            fsm.InitializeBehavior(data.RuntimeAIType);
        }
    }

    // ================================================================
    // [3] 행동 (Action) 관련
    // ================================================================

    // 행동 시작
    public void StartActionBehavior(PrisonerAIType type)
    {
        if (animator != null) animator.SetInteger("ActionType", GetActionAnimID(type));
        if (sfx != null) sfx.PlayLoop(type);

        if (_propMap.TryGetValue(type, out GameObject prop))
        {
            if (prop != null) prop.SetActive(true);
        }
    }

    // 행동 종료
    public void StopActionBehavior()
    {
        if (animator != null) animator.SetInteger("ActionType", 0);
        if (sfx != null) sfx.StopLoop();

        foreach (var prop in _propMap.Values)
        {
            if (prop != null) prop.SetActive(false);
        }
    }

    private int GetActionAnimID(PrisonerAIType type)
    {
        return type switch
        {
            PrisonerAIType.Singing => 1,
            PrisonerAIType.Screaming => 2,
            PrisonerAIType.Mumbling => 3,
            PrisonerAIType.HammeringWall => 4,
            PrisonerAIType.Deadlift => 5,
            PrisonerAIType.Crying => 6,
            PrisonerAIType.Escaper => 7,
            PrisonerAIType.Graffiti => 8,
            PrisonerAIType.Ambusher => 9,
            _ => 0
        };
    }

    // ================================================================
    // [4] 피격 및 사망 처리
    // ================================================================

    public virtual bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (Data == null || Data.CurrentHealth <= 0) return false;

        // 1. 체력 데이터 깎기 (Controller의 역할)
        Data.CurrentHealth -= dmg;

        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection); // 사망 처리
        }
        else
        {
            // 2. ★ FSM에게 "맞았다"고 알리기 (이게 없으면 피는 튀는데 가만히 있음)
            fsm.OnDamaged(dmg, hitPoint, hitDirection);

            // 3. 비명 소리 등 (Controller가 관리하는 사운드)
            if (sfx != null) sfx.PlayHitAndRandomMoan();
        }
        return true;
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        fsm.ChangeState(fsm.DeadState);
        if (sfx != null) sfx.PlayRandomDieOnce();

        StopActionBehavior();

        if (ragdoll != null) ragdoll.ApplyImpact(hitPoint, hitDirection, RagdollImpactForce);
        PrisonerEventBus.RaisePrisonerDown(Data.ID);
    }
}