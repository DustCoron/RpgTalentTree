using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TalentTree/Talent Node")]
public class RPG_TalentNodeData : ScriptableObject
{
    [Header("Node Info")]
    public string nodeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public TalentNodeType nodeType = TalentNodeType.Minor;
    public int talentPointCost = 1;
    
    [Header("Position")]
    [Tooltip("Ring index starting at 1 for inner ring")] 
    public int ringIndex = 1;
    [Tooltip("Angle in degrees around the circle")] 
    public float angle;
    
    [Header("Requirements")]
    public List<RPG_TalentNodeData> prerequisites = new List<RPG_TalentNodeData>();
    public int requiredLevel = 1;
    public ClassArchetype requiredArchetype = ClassArchetype.Warrior;
    public bool requiresAllArchetypes = false; // если true, доступен всем архетипам
    
    [Header("Effects")]
    public List<TalentStatModifier> statModifiers = new List<TalentStatModifier>();
    public List<TalentAbility> abilities = new List<TalentAbility>();
    
    [Header("Advanced")]
    public bool isKeystone = false; // Особо мощные таланты
    public bool isMastery = false; // Мастерские узлы
    public TalentCategory category = TalentCategory.Combat;
    
    /// <summary>
    /// Применить эффекты узла к системе статов
    /// </summary>
    public void ApplyToStatSystem(AdvancedStatSystem statSystem, string source = "Talent")
    {
        foreach (var statMod in statModifiers)
        {
            var modifier = new StatModifier(
                statMod.statType,
                statMod.modifierType,
                statMod.value,
                $"{source}: {nodeName} - {statMod.description}"
            );
            
            // Добавляем условие если есть
            if (statMod.condition != null)
                modifier.SetCondition(statMod.condition);
                
            statSystem.AddModifier(modifier);
        }
    }
    
    /// <summary>
    /// Убрать эффекты узла из системы статов
    /// </summary>
    public void RemoveFromStatSystem(AdvancedStatSystem statSystem, string source = "Talent")
    {
        foreach (var statMod in statModifiers)
        {
            statSystem.RemoveModifiersWithSource($"{source}: {nodeName} - {statMod.description}");
        }
    }
    
    /// <summary>
    /// Проверить, доступен ли узел для данного архетипа
    /// </summary>
    public bool IsAvailableForArchetype(ClassArchetype archetype)
    {
        return requiresAllArchetypes || requiredArchetype == archetype;
    }
    
    /// <summary>
    /// Получить превью статов для UI
    /// </summary>
    public string GetStatsPreview()
    {
        if (statModifiers.Count == 0) return "";
        
        string preview = "";
        foreach (var statMod in statModifiers)
        {
            string sign = statMod.value >= 0 ? "+" : "";
            string valueStr = statMod.modifierType == StatModifier.ModifierType.Flat ? 
                $"{sign}{statMod.value:F0}" : 
                $"{sign}{statMod.value:F0}%";
            
            preview += $"{valueStr} {GetStatDisplayName(statMod.statType)}\n";
        }
        
        return preview.TrimEnd('\n');
    }
    
    string GetStatDisplayName(StatType statType)
    {
        // Более читаемые названия для UI
        switch (statType)
        {
            case StatType.PhysicalDamage: return "Physical Damage";
            case StatType.ElementalDamage: return "Elemental Damage";
            case StatType.CriticalChance: return "Critical Strike Chance";
            case StatType.CriticalMultiplier: return "Critical Strike Multiplier";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.CastSpeed: return "Cast Speed";
            case StatType.LifeRegeneration: return "Life Regeneration";
            case StatType.ManaRegeneration: return "Mana Regeneration";
            case StatType.MovementSpeed: return "Movement Speed";
            default: return statType.ToString();
        }
    }
}

[System.Serializable]
public class TalentStatModifier
{
    public StatType statType;
    public StatModifier.ModifierType modifierType = StatModifier.ModifierType.Flat;
    public float value;
    public string description;
    public ModifierCondition condition; // Опциональное условие
}

[System.Serializable]
public class TalentAbility
{
    public string abilityName;
    [TextArea(2, 3)]
    public string description;
    public Sprite icon;
    public AbilityType type = AbilityType.Passive;
    public float cooldown = 0f;
    public int manaCost = 0;
}

public enum TalentNodeType
{
    Minor,      // Обычные узлы, 1 очко
    Notable,    // Заметные узлы, более мощные эффекты
    Keystone,   // Ключевые узлы, кардинально меняющие стиль игры
    Mastery,    // Мастерские узлы, специализация
    Jewel       // Слоты для самоцветов
}

public enum TalentCategory
{
    Combat,     // Боевые таланты
    Defense,    // Защитные таланты
    Magic,      // Магические таланты
    Utility,    // Утилитарные таланты
    Minion,     // Таланты миньонов
    Aura,       // Ауры и баффы
    Curse       // Проклятия и дебаффы
}

public enum AbilityType
{
    Passive,    // Пассивные способности
    Active,     // Активные способности
    Toggle,     // Переключаемые способности
    Triggered   // Срабатывающие при условии
}
