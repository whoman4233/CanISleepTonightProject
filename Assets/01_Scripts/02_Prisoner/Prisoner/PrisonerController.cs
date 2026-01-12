using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    private const float RagdollImpactForce = 10f; // 매직넘버 제거
    // ================================================================
    // [1] 데이터 정의 (인스펙터 매핑용 구조체)
    // ================================================================

    [System.Serializable]
    public struct VisualSkinData
    {
        public VisualAnomalyType type; // 예: BikiniModel
        public GameObject modelObject; // 해당 모델 오브젝트 (Hierarchy에 있는 자식 객체)
    }

    [System.Serializable]
    public struct ActionPropData
    {
        public PrisonerAIType type;    // 예: HammeringWall
        public GameObject propObject;  // 예: Hammer (손에 쥐어준 것)
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

    [Header("Visual Settings (Skins)")]
    [SerializeField] private GameObject defaultSkin; // 기본 죄수 모델
    [SerializeField] private List<VisualSkinData> specialSkins; // 특수 외형 매핑 리스트

    [Header("Action Props (Tools)")]
    [SerializeField] private List<ActionPropData> actionProps; // 행동 도구 매핑 리스트

    // 빠른 검색을 위한 딕셔너리 (Start 시점에 리스트 -> 딕셔너리 변환)
    private Dictionary<VisualAnomalyType, GameObject> _skinMap;
    private Dictionary<PrisonerAIType, GameObject> _propMap;

    public bool IsSuspicious { get; private set; }
    public PrisonerAIType AIType => Data != null ? Data.RuntimeAIType : PrisonerAIType.Good;

    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // FSM 설정
        fsm = GetComponent<PrisonerFSM>();
        if (fsm == null) fsm = gameObject.AddComponent<PrisonerFSM>();

        // ★ [핵심] 리스트 데이터를 딕셔너리로 변환 (성능 최적화)
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        // 1. 스킨 맵핑
        _skinMap = new Dictionary<VisualAnomalyType, GameObject>();
        if (specialSkins != null)
        {
            foreach (var data in specialSkins)
            {
                if (data.modelObject != null && !_skinMap.ContainsKey(data.type))
                    _skinMap.Add(data.type, data.modelObject);
            }
        }

        // 2. 도구 맵핑
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

        // 1. 외형(Visual) 적용 (Switch문 없이 데이터 기반으로 처리!)
        if (data != null && data.dailyRole.visualType != VisualAnomalyType.None)
        {
            ApplyVisualAnomaly(data.dailyRole.visualType);
        }
        else
        {
            ApplyVisualAnomaly(VisualAnomalyType.None);
        }

        // 2. FSM 초기화
        if (fsm != null)
        {
            fsm.Setup(this, agent, animator);
            fsm.InitializeBehavior(data.RuntimeAIType);
        }
    }

    // ================================================================
    // [3] 통합된 기능 함수들 (Visual & Action)
    // ================================================================

    // 외형 변경 로직
    private void ApplyVisualAnomaly(VisualAnomalyType visualType)
    {
        // 일단 모든 특수 스킨 끄기
        foreach (var skin in _skinMap.Values)
        {
            if (skin != null) skin.SetActive(false);
        }

        // 딕셔너리에서 해당 타입 스킨 찾아서 켜기
        if (_skinMap.TryGetValue(visualType, out GameObject targetSkin))
        {
            if (defaultSkin != null) defaultSkin.SetActive(false); // 기본 스킨 끄기
            targetSkin.SetActive(true);
        }
        else
        {
            // 해당하는 게 없으면(None 포함) 기본 스킨 켜기
            if (defaultSkin != null) defaultSkin.SetActive(true);
        }
    }

    // 행동 시작 (FSM -> ActionState에서 호출)
    public void StartActionBehavior(PrisonerAIType type)
    {
        // 1. 애니메이션 설정
        if (animator != null) animator.SetInteger("ActionType", GetActionAnimID(type));

        // 2. 소리 재생 (SfxController 위임)
        if (sfx != null) sfx.PlayLoop(type);

        // 3. 도구(Prop) 들기
        if (_propMap.TryGetValue(type, out GameObject prop))
        {
            if (prop != null) prop.SetActive(true);
        }
    }

    // 행동 종료
    public void StopActionBehavior()
    {
        // 1. 애니메이션 복구
        if (animator != null) animator.SetInteger("ActionType", 0);

        // 2. 소리 끄기
        if (sfx != null) sfx.StopLoop();

        // 3. 도구 숨기기 (켜져 있는 것만 꺼도 되지만 전체 순회로 확실하게)
        foreach (var prop in _propMap.Values)
        {
            if (prop != null) prop.SetActive(false);
        }
    }

    // 애니메이션 ID 매핑 (이건 규칙이므로 유지)
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
        if (Data == null)
        {
            Debug.LogError($"[PrisonerController] Data is NULL. Initialize()가 호출되지 않았습니다. 대상: {name}", this);
            return false;
        }

        if (fsm == null)
        {
            Debug.LogError($"[PrisonerController] FSM is NULL. 대상: {name}", this);
            return false;
        }

        if (Data.CurrentHealth <= 0 || fsm.IsInvulnerable) return false;

        Data.CurrentHealth -= dmg;

        if (Data.CurrentHealth <= 0)
        {
            Data.CurrentHealth = 0;
            Die(hitPoint, hitDirection);
        }
        else
        {
            fsm.OnDamaged(dmg, hitPoint, hitDirection);
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