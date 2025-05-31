using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Готовые шаблоны талантов для быстрого создания деревьев
/// </summary>
public static class TalentTemplates
{
    /// <summary>
    /// Создать дерево талантов для воина
    /// </summary>
    public static RPG_TalentTreeData CreateWarriorTree()
    {
        var tree = ScriptableObject.CreateInstance<RPG_TalentTreeData>();
        tree.treeName = "Warrior Combat";
        tree.description = "Physical damage and defense specialization";
        tree.primaryArchetype = ClassArchetype.Warrior;
        tree.maxTalentPoints = 80;
        
        var nodes = new List<RPG_TalentNodeData>();
        
        // Стартовые узлы (Ring 1)
        nodes.Add(CreateNode("Weapon Mastery", "Increased accuracy with all weapons", 
            new TalentStatModifier { statType = StatType.AccuracyRating, modifierType = StatModifier.ModifierType.Flat, value = 15f, description = "Weapon training" },
            1, 0f, TalentNodeType.Minor, TalentCategory.Combat));
            
        nodes.Add(CreateNode("Toughness", "Increased life and armor", 
            new TalentStatModifier { statType = StatType.Life, modifierType = StatModifier.ModifierType.Flat, value = 20f, description = "Physical conditioning" },
            1, 120f, TalentNodeType.Minor, TalentCategory.Defense));
            
        nodes.Add(CreateNode("Berserker's Fury", "Increased attack speed but reduced armor", 
            new TalentStatModifier { statType = StatType.AttackSpeed, modifierType = StatModifier.ModifierType.PercentIncrease, value = 20f, description = "Fury" },
            1, 240f, TalentNodeType.Minor, TalentCategory.Combat));
        
        // Ring 2 - Notable узлы
        nodes.Add(CreateNode("Two-Handed Mastery", "Specialized training with two-handed weapons", 
            new TalentStatModifier { statType = StatType.PhysicalDamage, modifierType = StatModifier.ModifierType.PercentIncrease, value = 25f, description = "Two-handed expertise" },
            2, 30f, TalentNodeType.Notable, TalentCategory.Combat, nodes[0])); // Prerequisites: Weapon Mastery
            
        nodes.Add(CreateNode("Shield Wall", "Massive increase to block chance and armor", 
            new TalentStatModifier { statType = StatType.BlockChance, modifierType = StatModifier.ModifierType.Flat, value = 25f, description = "Shield techniques" },
            2, 90f, TalentNodeType.Notable, TalentCategory.Defense, nodes[1])); // Prerequisites: Toughness
            
        nodes.Add(CreateNode("Bloodlust", "Life leech and increased critical chance", 
            new TalentStatModifier { statType = StatType.LifeLeech, modifierType = StatModifier.ModifierType.Flat, value = 5f, description = "Blood magic" },
            2, 210f, TalentNodeType.Notable, TalentCategory.Combat, nodes[2])); // Prerequisites: Berserker's Fury
        
        // Ring 3 - Keystone узлы
        nodes.Add(CreateNode("Unwavering Stance", "Cannot be stunned, but cannot evade attacks", 
            new TalentStatModifier { statType = StatType.Evasion, modifierType = StatModifier.ModifierType.PercentLess, value = 100f, description = "Immovable" },
            3, 60f, TalentNodeType.Keystone, TalentCategory.Defense, nodes[4])); // Prerequisites: Shield Wall
        nodes[6].isKeystone = true;
        
        tree.nodes = nodes;
        return tree;
    }
    
    /// <summary>
    /// Создать дерево талантов для мага
    /// </summary>
    public static RPG_TalentTreeData CreateMageTree()
    {
        var tree = ScriptableObject.CreateInstance<RPG_TalentTreeData>();
        tree.treeName = "Arcane Arts";
        tree.description = "Elemental magic and spell mastery";
        tree.primaryArchetype = ClassArchetype.Mage;
        tree.maxTalentPoints = 85;
        
        var nodes = new List<RPG_TalentNodeData>();
        
        // Ring 1 - Стартовые узлы
        nodes.Add(CreateNode("Mana Efficiency", "Reduced mana costs for all spells", 
            new TalentStatModifier { statType = StatType.ManaCostReduction, modifierType = StatModifier.ModifierType.Flat, value = 8f, description = "Efficient casting" },
            1, 0f, TalentNodeType.Minor, TalentCategory.Magic));
            
        nodes.Add(CreateNode("Elemental Focus", "Increased elemental damage", 
            new TalentStatModifier { statType = StatType.ElementalDamage, modifierType = StatModifier.ModifierType.PercentIncrease, value = 15f, description = "Elemental mastery" },
            1, 90f, TalentNodeType.Minor, TalentCategory.Magic));
            
        nodes.Add(CreateNode("Spell Echo", "Faster casting speed", 
            new TalentStatModifier { statType = StatType.CastSpeed, modifierType = StatModifier.ModifierType.PercentIncrease, value = 12f, description = "Quick casting" },
            1, 180f, TalentNodeType.Minor, TalentCategory.Magic));
            
        nodes.Add(CreateNode("Arcane Shield", "Energy shield and mana regeneration", 
            new TalentStatModifier { statType = StatType.EnergyShield, modifierType = StatModifier.ModifierType.Flat, value = 30f, description = "Protective magic" },
            1, 270f, TalentNodeType.Minor, TalentCategory.Defense));
        
        // Ring 2 - Notable узлы
        nodes.Add(CreateNode("Fire Mastery", "Specialized fire magic training", 
            new TalentStatModifier { statType = StatType.FireResistance, modifierType = StatModifier.ModifierType.Flat, value = 20f, description = "Fire immunity" },
            2, 45f, TalentNodeType.Notable, TalentCategory.Magic, nodes[1])); // Prerequisites: Elemental Focus
            
        nodes.Add(CreateNode("Critical Strikes", "Spell critical chance and multiplier", 
            new TalentStatModifier { statType = StatType.CriticalChance, modifierType = StatModifier.ModifierType.Flat, value = 8f, description = "Precise casting" },
            2, 135f, TalentNodeType.Notable, TalentCategory.Magic, nodes[2])); // Prerequisites: Spell Echo
            
        nodes.Add(CreateNode("Mana Shield", "Damage taken from mana before life", 
            new TalentStatModifier { statType = StatType.EnergyShieldRegeneration, modifierType = StatModifier.ModifierType.Flat, value = 15f, description = "Mana protection" },
            2, 315f, TalentNodeType.Notable, TalentCategory.Defense, nodes[3])); // Prerequisites: Arcane Shield
        
        // Ring 3 - Keystone
        nodes.Add(CreateNode("Elemental Overload", "Elemental damage penetrates resistances", 
            new TalentStatModifier { statType = StatType.ElementalDamage, modifierType = StatModifier.ModifierType.PercentMore, value = 40f, description = "Overwhelming power" },
            3, 90f, TalentNodeType.Keystone, TalentCategory.Magic, nodes[4], nodes[5])); // Prerequisites: Fire Mastery, Critical Strikes
        nodes[7].isKeystone = true;
        
        tree.nodes = nodes;
        return tree;
    }
    
    /// <summary>
    /// Создать дерево талантов для некроманта
    /// </summary>
    public static RPG_TalentTreeData CreateNecromancerTree()
    {
        var tree = ScriptableObject.CreateInstance<RPG_TalentTreeData>();
        tree.treeName = "Death Magic";
        tree.description = "Minion mastery and life manipulation";
        tree.primaryArchetype = ClassArchetype.Summoner;
        tree.maxTalentPoints = 90;
        
        var nodes = new List<RPG_TalentNodeData>();
        
        // Ring 1 - Стартовые узлы
        nodes.Add(CreateNode("Minion Mastery", "Increased minion life and damage", 
            new TalentStatModifier { statType = StatType.MinionLife, modifierType = StatModifier.ModifierType.PercentIncrease, value = 20f, description = "Undead control" },
            1, 0f, TalentNodeType.Minor, TalentCategory.Minion));
            
        nodes.Add(CreateNode("Death Magic", "Life leech and chaos damage", 
            new TalentStatModifier { statType = StatType.ChaosDamage, modifierType = StatModifier.ModifierType.PercentIncrease, value = 18f, description = "Death energy" },
            1, 120f, TalentNodeType.Minor, TalentCategory.Magic));
            
        nodes.Add(CreateNode("Corpse Magic", "Benefits from enemy deaths", 
            new TalentStatModifier { statType = StatType.ManaRegeneration, modifierType = StatModifier.ModifierType.Flat, value = 3f, description = "Soul harvest" },
            1, 240f, TalentNodeType.Minor, TalentCategory.Utility));
        
        // Ring 2 - Notable узлы
        nodes.Add(CreateNode("Skeletal Army", "Increased minion count and armor", 
            new TalentStatModifier { statType = StatType.MinionArmour, modifierType = StatModifier.ModifierType.Flat, value = 50f, description = "Bone reinforcement" },
            2, 60f, TalentNodeType.Notable, TalentCategory.Minion, nodes[0])); // Prerequisites: Minion Mastery
            
        nodes.Add(CreateNode("Life Drain", "Powerful life leech abilities", 
            new TalentStatModifier { statType = StatType.LifeLeech, modifierType = StatModifier.ModifierType.Flat, value = 8f, description = "Life siphon" },
            2, 180f, TalentNodeType.Notable, TalentCategory.Magic, nodes[1])); // Prerequisites: Death Magic
        
        // Ring 3 - Keystone
        nodes.Add(CreateNode("Lord of the Undead", "Massive minion bonuses but cannot regenerate life naturally", 
            new TalentStatModifier { statType = StatType.MinionDamage, modifierType = StatModifier.ModifierType.PercentMore, value = 100f, description = "Undead mastery" },
            3, 120f, TalentNodeType.Keystone, TalentCategory.Minion, nodes[3], nodes[4])); // Prerequisites: Skeletal Army, Life Drain
        nodes[5].isKeystone = true;
        
        tree.nodes = nodes;
        return tree;
    }
    
    /// <summary>
    /// Создать универсальное дерево утилитарных навыков
    /// </summary>
    public static RPG_TalentTreeData CreateUtilityTree()
    {
        var tree = ScriptableObject.CreateInstance<RPG_TalentTreeData>();
        tree.treeName = "Utility Skills";
        tree.description = "Universal skills available to all archetypes";
        tree.primaryArchetype = ClassArchetype.Warrior; // Но доступно всем через requiresAllArchetypes
        tree.maxTalentPoints = 60;
        
        var nodes = new List<RPG_TalentNodeData>();
        
        // Ring 1 - Базовые утилитарные навыки
        nodes.Add(CreateNode("Fleet Footed", "Increased movement speed", 
            new TalentStatModifier { statType = StatType.MovementSpeed, modifierType = StatModifier.ModifierType.PercentIncrease, value = 10f, description = "Swift movement" },
            1, 0f, TalentNodeType.Minor, TalentCategory.Utility));
        nodes[0].requiresAllArchetypes = true;
        
        nodes.Add(CreateNode("Lucky Find", "Increased item rarity and quantity", 
            new TalentStatModifier { statType = StatType.ItemRarity, modifierType = StatModifier.ModifierType.PercentIncrease, value = 25f, description = "Fortune's favor" },
            1, 180f, TalentNodeType.Minor, TalentCategory.Utility));
        nodes[1].requiresAllArchetypes = true;
        
        // Ring 2 - Notable утилитарные навыки
        nodes.Add(CreateNode("Student", "Increased experience gain", 
            new TalentStatModifier { statType = StatType.ExperienceGain, modifierType = StatModifier.ModifierType.PercentIncrease, value = 20f, description = "Faster learning" },
            2, 90f, TalentNodeType.Notable, TalentCategory.Utility, nodes[0], nodes[1])); // Prerequisites: Fleet Footed, Lucky Find
        nodes[2].requiresAllArchetypes = true;
        
        tree.nodes = nodes;
        return tree;
    }
    
    /// <summary>
    /// Вспомогательный метод для создания узла таланта
    /// </summary>
    static RPG_TalentNodeData CreateNode(string name, string description, TalentStatModifier statMod, 
        int ring, float angle, TalentNodeType type, TalentCategory category, params RPG_TalentNodeData[] prerequisites)
    {
        var node = ScriptableObject.CreateInstance<RPG_TalentNodeData>();
        node.nodeName = name;
        node.description = description;
        node.ringIndex = ring;
        node.angle = angle;
        node.nodeType = type;
        node.category = category;
        node.talentPointCost = type == TalentNodeType.Keystone ? 3 : type == TalentNodeType.Notable ? 2 : 1;
        
        node.statModifiers = new List<TalentStatModifier> { statMod };
        node.prerequisites = new List<RPG_TalentNodeData>(prerequisites);
        
        // Дополнительные модификаторы для Keystone узлов
        if (type == TalentNodeType.Keystone)
        {
            if (name == "Unwavering Stance")
            {
                // Добавляем второй модификатор - иммунитет к стану, но штраф к уклонению
                node.statModifiers.Add(new TalentStatModifier 
                { 
                    statType = StatType.Armour, 
                    modifierType = StatModifier.ModifierType.PercentIncrease, 
                    value = 50f, 
                    description = "Unwavering defense" 
                });
            }
            else if (name == "Lord of the Undead")
            {
                // Штраф к регенерации жизни
                node.statModifiers.Add(new TalentStatModifier 
                { 
                    statType = StatType.LifeRegeneration, 
                    modifierType = StatModifier.ModifierType.PercentLess, 
                    value = 100f, 
                    description = "Undead nature" 
                });
            }
        }
        
        return node;
    }
    
    /// <summary>
    /// Создать все базовые деревья для тестирования
    /// </summary>
    public static List<RPG_TalentTreeData> CreateAllBaseTrees()
    {
        return new List<RPG_TalentTreeData>
        {
            CreateWarriorTree(),
            CreateMageTree(),
            CreateNecromancerTree(),
            CreateUtilityTree()
        };
    }
} 