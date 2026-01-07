using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 리스트 검색(First, Find)을 위해 필요

[CreateAssetMenu(menuName = "GameData/Prisoner Database", fileName = "PrisonerDatabase")]
public class PrisonerDatabaseSO : ScriptableObject
{
    public List<PrisonerDefinition> prisoners = new();
    private Dictionary<string, PrisonerDefinition> _byTemplateId;

    public void RebuildIndex()
    {
        _byTemplateId = new Dictionary<string, PrisonerDefinition>();
        foreach (var p in prisoners)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.templateId)) continue;
            // 안전하게 중복 체크
            if (!_byTemplateId.ContainsKey(p.templateId))
                _byTemplateId[p.templateId] = p;
        }
    }

    // 기존 함수 유지
    public bool TryGet(string templateId, out PrisonerDefinition def)
    {
        if (_byTemplateId == null) RebuildIndex();
        return _byTemplateId.TryGetValue(templateId, out def);
    }

    public PrisonerDefinition GetRandomDefinition()
    {
        if (prisoners == null || prisoners.Count == 0) return null;
        return prisoners[Random.Range(0, prisoners.Count)];
    }

    // ========================================================================
    // ✅ [추가] VisualAnomalyType Enum을 받아서 프리팹을 찾는 함수
    // ========================================================================
    public GameObject GetPrefabByVisualType(VisualAnomalyType type)
    {
        // 1. Enum이 None이면 null 반환
        if (type == VisualAnomalyType.None) return null;

        // 2. Enum을 문자열(ID)로 변환 (예: Imposter_Guard -> "Imposter_Guard")
        string targetId = type.ToString();

        // 3. 인덱스가 없으면 빌드
        if (_byTemplateId == null) RebuildIndex();

        // 4. ID로 검색
        if (_byTemplateId.TryGetValue(targetId, out var def))
        {
            // PrisonerDefinition 안에 프리팹 변수명이 prisonerPrefab라고 가정
            return def.prisonerPrefab;
        }

        Debug.LogWarning($"[PrisonerDatabase] '{targetId}' ID를 가진 죄수 데이터를 찾을 수 없습니다! 오타를 확인하세요.");
        return null;
    }
}