using System;
using UnityEngine;

[Serializable]
public class PrisonerDefinition
{
    public string templateId;      // CSV: PrisonerID (Á¾·ù ID)
    public string displayName;     // CSV: PrisonerName
    public PrisonerAIType type;      // CSV: PrisonerType (Good/Bad)

    public int hp;                 // CSV: PrisonerHP
    public int atk;                // CSV: PrisonerATK
    public int spd;                // CSV: PrisonerSpd

    public bool isQte;             // CSV: IsQTE
    public string qteId;           // CSV: QTEID
    [TextArea] public string info; // CSV: Info
}
