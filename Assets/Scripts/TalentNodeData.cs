using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Node")]
public class TalentNodeData : ScriptableObject
{
    public string nodeName;
    [TextArea]
    public string description;
    [Tooltip("Ring index starting at 1 for inner ring")] public int ringIndex = 1;
    [Tooltip("Angle in degrees around the circle")] public float angle;
    public List<TalentNodeData> prerequisites = new List<TalentNodeData>();
}
