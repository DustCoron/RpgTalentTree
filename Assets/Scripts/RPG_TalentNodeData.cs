using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Node")]
public class RPG_TalentNodeData : ScriptableObject
{
    public string nodeName;
    [TextArea]
    public string description;
    public RPG_Basic_Stats stats;
    public List<RPG_SpellStats> spells = new List<RPG_SpellStats>();
    public List<RPG_MinionStats> minions = new List<RPG_MinionStats>();
    public int cost = 1;
    [Tooltip("Ring index starting at 1 for inner ring")] public int ringIndex = 1;
    [Tooltip("Angle in degrees around the circle")] public float angle;
    [Tooltip("Number of times this node is repeated around the ring")]
    public int repeatCount = 1;
    [Tooltip("Angular spacing in degrees for repeated nodes")]
    public float repeatSpacing = 10f;
    public List<RPG_TalentNodeData> prerequisites = new List<RPG_TalentNodeData>();
}
