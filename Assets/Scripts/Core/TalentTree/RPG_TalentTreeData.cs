using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Tree")]
public class RPG_TalentTreeData : ScriptableObject
{
    [Header("Tree Info")]
    public string treeName;
    [TextArea(2, 3)]
    public string description;
    public ClassArchetype primaryArchetype = ClassArchetype.Warrior;
    public Sprite treeIcon;
    
    [Header("Nodes")]
    public List<RPG_TalentNodeData> nodes = new List<RPG_TalentNodeData>();
    
    [Header("Layout")]
    public float ringSpacing = 100f;
    public int maxRings = 7;
    public Vector2 treeCenter = Vector2.zero;
    
    [Header("Balance")]
    public int maxTalentPoints = 100; // Максимум очков таланта в дереве
    public int startingTalentPoints = 0; // Стартовые очки
    public bool requiresUnlocking = false; // Требует ли дерево разблокировки
    
    /// <summary>
    /// Получить все узлы определенного типа
    /// </summary>
    public List<RPG_TalentNodeData> GetNodesByType(TalentNodeType type)
    {
        return nodes.FindAll(n => n.nodeType == type);
    }
    
    /// <summary>
    /// Получить все узлы определенной категории
    /// </summary>
    public List<RPG_TalentNodeData> GetNodesByCategory(TalentCategory category)
    {
        return nodes.FindAll(n => n.category == category);
    }
    
    /// <summary>
    /// Получить узлы в определенном кольце
    /// </summary>
    public List<RPG_TalentNodeData> GetNodesByRing(int ringIndex)
    {
        return nodes.FindAll(n => n.ringIndex == ringIndex);
    }
    
    /// <summary>
    /// Получить стартовые узлы (без предварительных условий)
    /// </summary>
    public List<RPG_TalentNodeData> GetStartingNodes()
    {
        return nodes.FindAll(n => n.prerequisites.Count == 0);
    }
    
    /// <summary>
    /// Проверить валидность дерева
    /// </summary>
    public bool ValidateTree(out string errorMessage)
    {
        errorMessage = "";
        
        // Проверяем наличие стартовых узлов
        var startingNodes = GetStartingNodes();
        if (startingNodes.Count == 0)
        {
            errorMessage = "Tree has no starting nodes (nodes without prerequisites)";
            return false;
        }
        
        // Проверяем циклические зависимости
        foreach (var node in nodes)
        {
            if (HasCircularDependency(node, new HashSet<RPG_TalentNodeData>()))
            {
                errorMessage = $"Circular dependency detected involving node: {node.nodeName}";
                return false;
            }
        }
        
        // Проверяем, что все предварительные условия указывают на существующие узлы
        foreach (var node in nodes)
        {
            foreach (var prereq in node.prerequisites)
            {
                if (!nodes.Contains(prereq))
                {
                    errorMessage = $"Node {node.nodeName} has prerequisite {prereq.nodeName} that is not in this tree";
                    return false;
                }
            }
        }
        
        return true;
    }
    
    bool HasCircularDependency(RPG_TalentNodeData node, HashSet<RPG_TalentNodeData> visited)
    {
        if (visited.Contains(node))
            return true;
            
        visited.Add(node);
        
        foreach (var prereq in node.prerequisites)
        {
            if (HasCircularDependency(prereq, new HashSet<RPG_TalentNodeData>(visited)))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Получить общее количество узлов каждого типа для баланса
    /// </summary>
    public TalentTreeStats GetTreeStats()
    {
        var stats = new TalentTreeStats();
        
        foreach (var node in nodes)
        {
            switch (node.nodeType)
            {
                case TalentNodeType.Minor: stats.minorNodes++; break;
                case TalentNodeType.Notable: stats.notableNodes++; break;
                case TalentNodeType.Keystone: stats.keystoneNodes++; break;
                case TalentNodeType.Mastery: stats.masteryNodes++; break;
                case TalentNodeType.Jewel: stats.jewelSlots++; break;
            }
            
            stats.totalCost += node.talentPointCost;
        }
        
        return stats;
    }
}

[System.Serializable]
public class TalentTreeStats
{
    public int minorNodes = 0;
    public int notableNodes = 0;
    public int keystoneNodes = 0;
    public int masteryNodes = 0;
    public int jewelSlots = 0;
    public int totalCost = 0;
    
    public override string ToString()
    {
        return $"Minor: {minorNodes}, Notable: {notableNodes}, Keystone: {keystoneNodes}, " +
               $"Mastery: {masteryNodes}, Jewel Slots: {jewelSlots}, Total Cost: {totalCost}";
    }
}
