using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Центральный менеджер талантов, управляющий изученными талантами и их эффектами
/// </summary>
public class TalentManager : MonoBehaviour
{
    [Header("Configuration")]
    public List<RPG_TalentTreeData> availableTrees = new List<RPG_TalentTreeData>();
    public int basePointsPerLevel = 1;
    public int bonusPointsAtMilestones = 1; // Бонусные очки на уровнях 10, 20, 30...
    
    [Header("Runtime")]
    public int currentLevel = 1;
    public int availableTalentPoints = 1;
    public int spentTalentPoints = 0;
    
    // Состояние талантов
    private HashSet<RPG_TalentNodeData> allocatedNodes = new HashSet<RPG_TalentNodeData>();
    private Dictionary<RPG_TalentTreeData, HashSet<RPG_TalentNodeData>> treeAllocations = new Dictionary<RPG_TalentTreeData, HashSet<RPG_TalentNodeData>>();
    
    // Интеграция с системами
    private AdvancedStatSystem statSystem;
    private PlayerCharacterController playerController;
    
    // События
    public System.Action<RPG_TalentNodeData> OnTalentAllocated;
    public System.Action<RPG_TalentNodeData> OnTalentDeallocated;
    public System.Action<int> OnTalentPointsChanged;
    
    void Awake()
    {
        // Инициализация словарей для каждого дерева
        foreach (var tree in availableTrees)
        {
            treeAllocations[tree] = new HashSet<RPG_TalentNodeData>();
        }
    }
    
    void Start()
    {
        // Находим компоненты
        playerController = GetComponent<PlayerCharacterController>();
        if (playerController != null)
            statSystem = playerController.statSystem;
        
        // Валидируем деревья
        ValidateAllTrees();
        
        // Загружаем сохраненное состояние
        LoadTalentState();
    }
    
    void ValidateAllTrees()
    {
        foreach (var tree in availableTrees)
        {
            if (tree.ValidateTree(out string error))
            {
                Debug.Log($"Talent tree '{tree.treeName}' validated successfully. Stats: {tree.GetTreeStats()}");
            }
            else
            {
                Debug.LogError($"Talent tree '{tree.treeName}' validation failed: {error}");
            }
        }
    }
    
    /// <summary>
    /// Попытаться изучить талант
    /// </summary>
    public bool TryAllocateTalent(RPG_TalentNodeData node)
    {
        if (!CanAllocateTalent(node, out string reason))
        {
            Debug.LogWarning($"Cannot allocate talent {node.nodeName}: {reason}");
            return false;
        }
        
        // Тратим очки таланта
        availableTalentPoints -= node.talentPointCost;
        spentTalentPoints += node.talentPointCost;
        
        // Добавляем в изученные
        allocatedNodes.Add(node);
        
        // Добавляем в соответствующее дерево
        var tree = GetTreeContaining(node);
        if (tree != null)
            treeAllocations[tree].Add(node);
        
        // Применяем эффекты к системе статов
        if (statSystem != null)
            node.ApplyToStatSystem(statSystem);
        
        // Уведомляем о изменении
        OnTalentAllocated?.Invoke(node);
        OnTalentPointsChanged?.Invoke(availableTalentPoints);
        
        Debug.Log($"Allocated talent: {node.nodeName} (Cost: {node.talentPointCost}, Remaining points: {availableTalentPoints})");
        return true;
    }
    
    /// <summary>
    /// Попытаться отменить изучение таланта
    /// </summary>
    public bool TryDeallocateTalent(RPG_TalentNodeData node)
    {
        if (!CanDeallocateTalent(node, out string reason))
        {
            Debug.LogWarning($"Cannot deallocate talent {node.nodeName}: {reason}");
            return false;
        }
        
        // Возвращаем очки таланта
        availableTalentPoints += node.talentPointCost;
        spentTalentPoints -= node.talentPointCost;
        
        // Убираем из изученных
        allocatedNodes.Remove(node);
        
        // Убираем из дерева
        var tree = GetTreeContaining(node);
        if (tree != null)
            treeAllocations[tree].Remove(node);
        
        // Убираем эффекты из системы статов
        if (statSystem != null)
            node.RemoveFromStatSystem(statSystem);
        
        // Уведомляем о изменении
        OnTalentDeallocated?.Invoke(node);
        OnTalentPointsChanged?.Invoke(availableTalentPoints);
        
        Debug.Log($"Deallocated talent: {node.nodeName} (Refund: {node.talentPointCost}, Available points: {availableTalentPoints})");
        return true;
    }
    
    /// <summary>
    /// Проверить, можно ли изучить талант
    /// </summary>
    public bool CanAllocateTalent(RPG_TalentNodeData node, out string reason)
    {
        reason = "";
        
        // Уже изучен
        if (IsAllocated(node))
        {
            reason = "Already allocated";
            return false;
        }
        
        // Недостаточно очков
        if (availableTalentPoints < node.talentPointCost)
        {
            reason = $"Insufficient talent points (need {node.talentPointCost}, have {availableTalentPoints})";
            return false;
        }
        
        // Недостаточный уровень
        if (currentLevel < node.requiredLevel)
        {
            reason = $"Level too low (need {node.requiredLevel}, current {currentLevel})";
            return false;
        }
        
        // Проверка архетипа
        if (playerController?.playerClass != null)
        {
            if (!node.IsAvailableForArchetype(playerController.playerClass.Archetype))
            {
                reason = $"Not available for {playerController.playerClass.Archetype} archetype";
                return false;
            }
        }
        
        // Проверка предварительных условий
        foreach (var prereq in node.prerequisites)
        {
            if (!IsAllocated(prereq))
            {
                reason = $"Prerequisite not met: {prereq.nodeName}";
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Проверить, можно ли отменить изучение таланта
    /// </summary>
    public bool CanDeallocateTalent(RPG_TalentNodeData node, out string reason)
    {
        reason = "";
        
        // Не изучен
        if (!IsAllocated(node))
        {
            reason = "Not allocated";
            return false;
        }
        
        // Проверяем, есть ли зависимые узлы
        var dependentNodes = GetDependentNodes(node);
        if (dependentNodes.Count > 0)
        {
            reason = $"Has dependent nodes: {string.Join(", ", dependentNodes.Select(n => n.nodeName))}";
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Получить узлы, зависящие от данного
    /// </summary>
    List<RPG_TalentNodeData> GetDependentNodes(RPG_TalentNodeData node)
    {
        var dependents = new List<RPG_TalentNodeData>();
        
        foreach (var allocatedNode in allocatedNodes)
        {
            if (allocatedNode.prerequisites.Contains(node))
                dependents.Add(allocatedNode);
        }
        
        return dependents;
    }
    
    /// <summary>
    /// Проверить, изучен ли талант
    /// </summary>
    public bool IsAllocated(RPG_TalentNodeData node)
    {
        return allocatedNodes.Contains(node);
    }
    
    /// <summary>
    /// Найти дерево, содержащее узел
    /// </summary>
    RPG_TalentTreeData GetTreeContaining(RPG_TalentNodeData node)
    {
        return availableTrees.FirstOrDefault(tree => tree.nodes.Contains(node));
    }
    
    /// <summary>
    /// Повысить уровень и получить очки талантов
    /// </summary>
    public void LevelUp()
    {
        currentLevel++;
        int pointsGained = basePointsPerLevel;
        
        // Бонусные очки на особых уровнях
        if (currentLevel % 10 == 0)
            pointsGained += bonusPointsAtMilestones;
        
        availableTalentPoints += pointsGained;
        OnTalentPointsChanged?.Invoke(availableTalentPoints);
        
        Debug.Log($"Level up! New level: {currentLevel}, Talent points gained: {pointsGained}");
    }
    
    /// <summary>
    /// Сброс всех талантов
    /// </summary>
    public void ResetAllTalents()
    {
        // Убираем все эффекты из системы статов
        if (statSystem != null)
        {
            foreach (var node in allocatedNodes)
                node.RemoveFromStatSystem(statSystem);
        }
        
        // Возвращаем очки
        availableTalentPoints += spentTalentPoints;
        spentTalentPoints = 0;
        
        // Очищаем все изученные
        allocatedNodes.Clear();
        foreach (var tree in treeAllocations.Keys.ToList())
            treeAllocations[tree].Clear();
        
        OnTalentPointsChanged?.Invoke(availableTalentPoints);
        Debug.Log("All talents reset");
    }
    
    /// <summary>
    /// Получить статистику по дереву
    /// </summary>
    public TalentTreeAllocationStats GetTreeAllocationStats(RPG_TalentTreeData tree)
    {
        if (!treeAllocations.ContainsKey(tree))
            return new TalentTreeAllocationStats();
        
        var stats = new TalentTreeAllocationStats();
        var allocated = treeAllocations[tree];
        
        foreach (var node in allocated)
        {
            switch (node.nodeType)
            {
                case TalentNodeType.Minor: stats.minorAllocated++; break;
                case TalentNodeType.Notable: stats.notableAllocated++; break;
                case TalentNodeType.Keystone: stats.keystoneAllocated++; break;
                case TalentNodeType.Mastery: stats.masteryAllocated++; break;
                case TalentNodeType.Jewel: stats.jewelAllocated++; break;
            }
            
            stats.totalPointsSpent += node.talentPointCost;
        }
        
        stats.totalAllocated = allocated.Count;
        return stats;
    }
    
    /// <summary>
    /// Сохранить состояние талантов
    /// </summary>
    public void SaveTalentState()
    {
        var saveData = new TalentSaveData
        {
            currentLevel = currentLevel,
            availableTalentPoints = availableTalentPoints,
            spentTalentPoints = spentTalentPoints,
            allocatedNodeNames = allocatedNodes.Select(n => n.nodeName).ToList()
        };
        
        string json = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString("TalentSaveData", json);
        Debug.Log("Talent state saved");
    }
    
    /// <summary>
    /// Загрузить состояние талантов
    /// </summary>
    public void LoadTalentState()
    {
        string json = PlayerPrefs.GetString("TalentSaveData", "");
        if (string.IsNullOrEmpty(json)) return;
        
        try
        {
            var saveData = JsonUtility.FromJson<TalentSaveData>(json);
            currentLevel = saveData.currentLevel;
            availableTalentPoints = saveData.availableTalentPoints;
            spentTalentPoints = saveData.spentTalentPoints;
            
            // Восстанавливаем изученные таланты
            foreach (var nodeName in saveData.allocatedNodeNames)
            {
                var node = FindNodeByName(nodeName);
                if (node != null)
                {
                    allocatedNodes.Add(node);
                    
                    var tree = GetTreeContaining(node);
                    if (tree != null)
                        treeAllocations[tree].Add(node);
                    
                    // Применяем эффекты
                    if (statSystem != null)
                        node.ApplyToStatSystem(statSystem);
                }
            }
            
            Debug.Log($"Talent state loaded: Level {currentLevel}, Points {availableTalentPoints}/{spentTalentPoints}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load talent state: {e.Message}");
        }
    }
    
    RPG_TalentNodeData FindNodeByName(string nodeName)
    {
        foreach (var tree in availableTrees)
        {
            var node = tree.nodes.FirstOrDefault(n => n.nodeName == nodeName);
            if (node != null) return node;
        }
        return null;
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveTalentState();
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveTalentState();
    }
}

[System.Serializable]
public class TalentTreeAllocationStats
{
    public int totalAllocated = 0;
    public int totalPointsSpent = 0;
    public int minorAllocated = 0;
    public int notableAllocated = 0;
    public int keystoneAllocated = 0;
    public int masteryAllocated = 0;
    public int jewelAllocated = 0;
}

[System.Serializable]
public class TalentSaveData
{
    public int currentLevel;
    public int availableTalentPoints;
    public int spentTalentPoints;
    public List<string> allocatedNodeNames = new List<string>();
} 