using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Tree")]
public class RPG_TalentTreeData : ScriptableObject
{
    public List<RPG_TalentNodeData> nodes = new List<RPG_TalentNodeData>();
    public float ringSpacing = 100f;
}
