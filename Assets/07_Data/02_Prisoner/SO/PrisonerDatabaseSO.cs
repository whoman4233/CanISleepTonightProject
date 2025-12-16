using System.Collections.Generic;
using UnityEngine;

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
            _byTemplateId[p.templateId] = p;
        }
    }

    public bool TryGet(string templateId, out PrisonerDefinition def)
    {
        if (_byTemplateId == null) RebuildIndex();
        return _byTemplateId.TryGetValue(templateId, out def);
    }
}
