using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Anomaly Database", fileName = "AnomalyDatabase")]
public class AnomalyDatabaseSO : ScriptableObject
{
    public List<AnomalyDefinitionSO> defs = new();
}