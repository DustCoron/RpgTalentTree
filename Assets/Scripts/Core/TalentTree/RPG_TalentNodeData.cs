using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Node")]
public class RPG_TalentNodeData : ScriptableObject
{
    public string nodeName;
    [TextArea]
    public string description;
    public RPG_Basic_Stats stats;
    [Tooltip("Ring index starting at 1 for inner ring")] public int ringIndex = 1;
    [Tooltip("Angle in degrees around the circle")] public float angle;
    public List<RPG_TalentNodeData> prerequisites = new List<RPG_TalentNodeData>();
}
