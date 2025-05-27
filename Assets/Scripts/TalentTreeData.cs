using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Tree")]
public class TalentTreeData : ScriptableObject
{
    public List<TalentNodeData> nodes = new List<TalentNodeData>();
    public float ringSpacing = 100f;
}
