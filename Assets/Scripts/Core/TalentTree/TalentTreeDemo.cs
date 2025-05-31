using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Demo script demonstrating the Talent Tree System functionality
/// Add this to a GameObject to test the talent system
/// </summary>
public class TalentTreeDemo : MonoBehaviour
{
    [Header("Demo Configuration")]
    public bool runDemoOnStart = true;
    public bool logDetailedInfo = true;
    
    // References
    private TalentManager talentManager;
    private AdvancedStatSystem statSystem;
    private List<RPG_TalentTreeData> demoTrees;
    
    void Start()
    {
        if (runDemoOnStart)
        {
            SetupDemo();
            RunDemo();
        }
    }
    
    void SetupDemo()
    {
        Debug.Log("=== TALENT TREE SYSTEM DEMO ===");
        
        // Create stat system
        statSystem = new AdvancedStatSystem();
        
        // Create talent manager component
        var talentManagerGO = new GameObject("TalentManager");
        talentManager = talentManagerGO.AddComponent<TalentManager>();
        
        // Create demo trees using templates
        demoTrees = TalentTemplates.CreateAllBaseTrees();
        talentManager.availableTrees = demoTrees;
        
        // Setup demo character
        talentManager.currentLevel = 10; // Level 10 character
        talentManager.availableTalentPoints = 15; // Plenty of points to spend
        
        Debug.Log($"Demo setup complete: Level {talentManager.currentLevel}, {talentManager.availableTalentPoints} talent points");
        
        if (logDetailedInfo)
        {
            LogTreeInformation();
        }
    }
    
    void RunDemo()
    {
        Debug.Log("\n=== RUNNING TALENT ALLOCATION DEMO ===");
        
        // Test warrior tree
        var warriorTree = demoTrees.FirstOrDefault(t => t.treeName == "Warrior Combat");
        if (warriorTree != null)
        {
            DemoWarriorTalents(warriorTree);
        }
        
        // Test mage tree
        var mageTree = demoTrees.FirstOrDefault(t => t.treeName == "Arcane Arts");
        if (mageTree != null)
        {
            DemoMageTalents(mageTree);
        }
        
        // Show final stats
        Debug.Log("\n=== FINAL DEMO RESULTS ===");
        Debug.Log($"Remaining talent points: {talentManager.availableTalentPoints}");
        Debug.Log($"Total spent: {talentManager.spentTalentPoints}");
        
        // Show allocated talents
        var allocatedTalents = GetAllocatedTalents();
        Debug.Log($"Allocated talents ({allocatedTalents.Count}):");
        foreach (var talent in allocatedTalents)
        {
            Debug.Log($"  - {talent.nodeName} ({talent.nodeType}, {talent.talentPointCost} points)");
        }
    }
    
    void DemoWarriorTalents(RPG_TalentTreeData warriorTree)
    {
        Debug.Log($"\n--- Testing {warriorTree.treeName} ---");
        
        // Try to allocate some warrior talents
        var weaponMastery = warriorTree.nodes.FirstOrDefault(n => n.nodeName == "Weapon Mastery");
        var toughness = warriorTree.nodes.FirstOrDefault(n => n.nodeName == "Toughness");
        var twoHandedMastery = warriorTree.nodes.FirstOrDefault(n => n.nodeName == "Two-Handed Mastery");
        
        // Test basic talent allocation
        if (weaponMastery != null)
        {
            bool success = talentManager.TryAllocateTalent(weaponMastery);
            Debug.Log($"Weapon Mastery allocation: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success && logDetailedInfo)
            {
                Debug.Log($"  Stats preview: {weaponMastery.GetStatsPreview()}");
            }
        }
        
        if (toughness != null)
        {
            bool success = talentManager.TryAllocateTalent(toughness);
            Debug.Log($"Toughness allocation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        // Test prerequisite system
        if (twoHandedMastery != null)
        {
            bool success = talentManager.TryAllocateTalent(twoHandedMastery);
            Debug.Log($"Two-Handed Mastery allocation (requires Weapon Mastery): {(success ? "SUCCESS" : "FAILED")}");
        }
    }
    
    void DemoMageTalents(RPG_TalentTreeData mageTree)
    {
        Debug.Log($"\n--- Testing {mageTree.treeName} ---");
        
        var manaEfficiency = mageTree.nodes.FirstOrDefault(n => n.nodeName == "Mana Efficiency");
        var elementalFocus = mageTree.nodes.FirstOrDefault(n => n.nodeName == "Elemental Focus");
        
        if (manaEfficiency != null)
        {
            bool success = talentManager.TryAllocateTalent(manaEfficiency);
            Debug.Log($"Mana Efficiency allocation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        if (elementalFocus != null)
        {
            bool success = talentManager.TryAllocateTalent(elementalFocus);
            Debug.Log($"Elemental Focus allocation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        // Test archetype restriction (should fail if character is not a mage)
        Debug.Log("Note: Mage talents may fail due to archetype restrictions in demo");
    }
    
    void LogTreeInformation()
    {
        Debug.Log("\n=== TALENT TREE INFORMATION ===");
        
        foreach (var tree in demoTrees)
        {
            var stats = tree.GetTreeStats();
            var validation = tree.ValidateTree(out string error);
            
            Debug.Log($"\n{tree.treeName} ({tree.primaryArchetype}):");
            Debug.Log($"  Validation: {(validation ? "PASSED" : "FAILED")}");
            if (!validation) Debug.LogWarning($"  Error: {error}");
            Debug.Log($"  Stats: {stats}");
            Debug.Log($"  Total Nodes: {tree.nodes.Count}");
            Debug.Log($"  Max Points: {tree.maxTalentPoints}");
            
            if (logDetailedInfo)
            {
                Debug.Log($"  Starting nodes: {string.Join(", ", tree.GetStartingNodes().Select(n => n.nodeName))}");
                Debug.Log($"  Keystone nodes: {string.Join(", ", tree.GetNodesByType(TalentNodeType.Keystone).Select(n => n.nodeName))}");
            }
        }
    }
    
    List<RPG_TalentNodeData> GetAllocatedTalents()
    {
        var allocated = new List<RPG_TalentNodeData>();
        
        foreach (var tree in demoTrees)
        {
            foreach (var node in tree.nodes)
            {
                if (talentManager.IsAllocated(node))
                {
                    allocated.Add(node);
                }
            }
        }
        
        return allocated;
    }
    
    // Public methods for manual testing
    [ContextMenu("Run Demo")]
    public void RunDemoManual()
    {
        if (talentManager == null) SetupDemo();
        RunDemo();
    }
    
    [ContextMenu("Reset All Talents")]
    public void ResetTalents()
    {
        if (talentManager != null)
        {
            talentManager.ResetAllTalents();
            Debug.Log("All talents reset!");
        }
    }
    
    [ContextMenu("Add Talent Points")]
    public void AddTalentPoints()
    {
        if (talentManager != null)
        {
            talentManager.availableTalentPoints += 10;
            Debug.Log($"Added 10 talent points. Total: {talentManager.availableTalentPoints}");
        }
    }
    
    [ContextMenu("Level Up")]
    public void LevelUpDemo()
    {
        if (talentManager != null)
        {
            talentManager.LevelUp();
            Debug.Log($"Leveled up! Now level {talentManager.currentLevel}");
        }
    }
} 